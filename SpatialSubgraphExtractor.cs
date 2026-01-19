using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Geometry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;


public enum LifeCycleStatus : Int16
{
    Unknown = 0,
    PlannedInstalled = 1,
    InService = 2,
    Decommissioned = 3,
    PlannedUninstalled = 4
}

public enum NormalOperatingStatus : Int16
{
    Closed = 0,
    Open = 1,
    OpenAndTraceble = 2
}



[Flags]
public enum AssociationStatus
{
    // ---- 基础位（原子标志） ----
    None = 0,        // 0
    Container = 1 << 0,   // 1
    Structure = 1 << 1,   // 2
    Content = 1 << 2,   // 4
    Attachment = 1 << 3,   // 8
    VisibleContent = 1 << 4,   // 16
    Connectivity = 1 << 5,   // 32

    // ---- 组合别名（按你的清单列出，便于直观使用） ----

    // 单独可见内容/附件与容器或结构
    VisibleContent_Attachment_Container = VisibleContent | Attachment | Container,           // 25
    VisibleContent_Attachment_Structure = VisibleContent | Attachment | Structure,           // 26

    // Connectivity + Container/Structure/Content 及其扩展
    Connectivity_Container = Connectivity | Container,                          // 33
    Connectivity_Structure = Connectivity | Structure,                          // 34
    Connectivity_Content = Connectivity | Content,                            // 36
    Connectivity_Content_Container = Connectivity | Content | Container,                 // 37
    Connectivity_Content_Structure = Connectivity | Content | Structure,                 // 38

    Connectivity_Attachment = Connectivity | Attachment,                         // 40
    Connectivity_Attachment_Container = Connectivity | Attachment | Container,              // 41
    Connectivity_Attachment_Structure = Connectivity | Attachment | Structure,              // 42
    Connectivity_Attachment_Content = Connectivity | Attachment | Content,                // 44
    Connectivity_Attachment_Content_Container = Connectivity | Attachment | Content | Container,    // 45
    Connectivity_Attachment_Content_Structure = Connectivity | Attachment | Content | Structure,    // 46

    Connectivity_VisibleContent = Connectivity | VisibleContent,                      // 48
    Connectivity_VisibleContent_Container = Connectivity | VisibleContent | Container,          // 49
    Connectivity_VisibleContent_Structure = Connectivity | VisibleContent | Structure,          // 50
    Connectivity_VisibleContent_Attachment = Connectivity | VisibleContent | Attachment,         // 56
    Connectivity_VisibleContent_Attachment_Container = Connectivity | VisibleContent | Attachment | Container, // 57
    Connectivity_VisibleContent_Attachment_Structure = Connectivity | VisibleContent | Attachment | Structure  // 58
}

/// <summary>
/// 从 UN trace 结果中抽取“仅空间要素”，并为每个要素生成属性快照（可选包含 Geometry）。
/// 必须在 MCT 线程（QueuedTask.Run）中调用。
/// </summary>
public sealed class SpatialSubgraphExtractor
{
    private readonly UtilityNetwork _un;
    private readonly Options _opt;

    public sealed class Options
    {
        /// 想要缓存的字段名（跨图层公共字段名，大小写不敏感）。缺失会自动跳过。
        public IReadOnlyList<string> WantedFields { get; init; } = Array.Empty<string>();

        /// 是否抓取所有非几何字段（字段多时较重）。默认 false。
        public bool ReadAllNonGeometryFields { get; init; } = true;

        /// 是否总是包含 GlobalID/OBJECTID（建议保持 true）。
        public bool AlwaysIncludeKeys { get; init; } = true;

        /// 是否在快照中包含 Geometry（用于后续端点建图）。默认 true。
        public bool IncludeGeometry { get; init; } = true;
    }

    public SpatialSubgraphExtractor(UtilityNetwork un, Options opt = null)
    {
        _un = un ?? throw new ArgumentNullException(nameof(un));
        _opt = opt ?? new Options();
    }

    public SpatialSubgraph ExtractFromResults(IReadOnlyList<Result> traceResults)
    {
       
        var elements = traceResults
            .OfType<ElementResult>()
            .SelectMany(r => r.Elements)
            .Where(e => e != null && e.GlobalID != Guid.Empty)
            .GroupBy(e => e.GlobalID)
            .Select(g => g.First())
            .ToList();

        return Extract(elements);
    }

   

    /// <summary>直接从元素集合提取。</summary>
    public SpatialSubgraph Extract(IEnumerable<Element> elements)
    {
        var nodes = elements
            .Where(e => e != null && e.GlobalID != Guid.Empty)
            .GroupBy(e => e.GlobalID)
            .Select(g => g.First())
            .ToList();

        var elemByGid = nodes.ToDictionary(e => e.GlobalID);
        var snapshots = new Dictionary<Guid, FeatureSnapshot>();
        var spatialElems = new List<Element>();

        foreach (var grp in nodes.GroupBy(n => n.NetworkSource))
        {
            using var table = _un.GetTable(grp.Key);
            var def = table.GetDefinition();
            var fcDef = def as FeatureClassDefinition;
            if (fcDef == null) continue; // 非空间表，跳过

            var oidField = def.GetObjectIDField();
            var gidField = def.GetGlobalIDField();
            var shapeField = fcDef.GetShapeField();

            // 计算要读取的字段列表
            var tableFields = def.GetFields().Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var subFieldSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (_opt.AlwaysIncludeKeys)
            {
                subFieldSet.Add(oidField);
                subFieldSet.Add(gidField);
            }

            if (_opt.ReadAllNonGeometryFields)
            {
                foreach (var f in tableFields)
                    if (!f.Equals(shapeField, StringComparison.OrdinalIgnoreCase))
                        subFieldSet.Add(f);
            }
            else
            {
                foreach (var w in _opt.WantedFields ?? Array.Empty<string>())
                    if (tableFields.Contains(w))
                        subFieldSet.Add(w);
            }

            // 若需要几何，确保把 shapeField 也加入到 SubFields
            if (_opt.IncludeGeometry)
                subFieldSet.Add(shapeField);

            // 最少也要拿键字段
            if (subFieldSet.Count == 0)
            {
                subFieldSet.Add(oidField);
                subFieldSet.Add(gidField);
            }

            var oids = grp.Select(e => e.ObjectID).Distinct().ToList();
            if (oids.Count == 0) continue;

            var qf = new QueryFilter
            {
                ObjectIDs = oids,
                SubFields = string.Join(",", subFieldSet)
            };

            using var cursor = table.Search(qf, false);
            while (cursor.MoveNext())
            {
                if (cursor.Current is not Row row) continue;

                // 仅接受空间要素
                if (row is not Feature fea) continue;

                // GlobalID / OID
                var gidObj = row[gidField];
                Guid gid = Guid.Parse(gidObj as string);
                if (!elemByGid.ContainsKey(gid)) continue;

                var oid = Convert.ToInt64(row[oidField]);

                // 拍属性字典（剔除 shapeField）
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var f in subFieldSet)
                {
                    if (f.Equals(shapeField, StringComparison.OrdinalIgnoreCase)) continue;
                    dict[f] = row[f];
                }

                // 如需，取 Geometry；否则置 null
                Geometry geom = null;
                if (_opt.IncludeGeometry)
                {
                    try { geom = fea.GetShape(); } catch { geom = null; }
                }
                var elment = elemByGid[gid];

                snapshots[gid] = new FeatureSnapshot(
                    elment,
                    grp.Key,
                    tableName: table.GetName(),
                    attributes: dict,
                    geometry: geom
                );
                spatialElems.Add(elemByGid[gid]);
            }
        }

        return new SpatialSubgraph
        {
            SpatialElements = spatialElems,
            ElementByGlobalId = elemByGid,
            FeatureByGlobalId = snapshots
        };
    }


   

    /// <summary>
    /// 按快照再打开一次活的 Feature（需要更多字段或编辑时用）。调用方自行 using 释放。
    /// </summary>
    public Feature OpenFeature(FeatureSnapshot snap)
    {
        if (snap == null) return null;
        // 用 NetworkSourceName 找表更稳（也可用 TableName）
        var ns = _un.GetDefinition().GetNetworkSource(snap.NetworkSourceName);
        using var table = _un.GetTable(ns);
        var def = table.GetDefinition();
        var qf = new QueryFilter { ObjectIDs = new List<long> { snap.ObjectID } };
        var rc = table.Search(qf, false);
        return rc.MoveNext() ? (rc.Current as Feature) : null; // 注意：返回者需 using
    }
}

/// <summary>仅空间子图：元素集合 + GlobalID→FeatureSnapshot 映射。</summary>
public sealed class SpatialSubgraph
{
    public List<Element> SpatialElements { get; init; } = new();
    public Dictionary<Guid, Element> ElementByGlobalId { get; init; } = new();
    public Dictionary<Guid, FeatureSnapshot> FeatureByGlobalId { get; init; } = new();

    public IList<FeatureSnapshot> Features
    {
        get
        {
            return new List<FeatureSnapshot>(this.FeatureByGlobalId.Values);
        }
    }
}





/// <summary>要素“快照”：包含必要标识、选定属性，及可选几何。</summary>
public sealed class FeatureSnapshot
{
    public Guid GlobalID { get; set; }
    public long ObjectID { get; }
    [JsonIgnore]
    public string NetworkSourceName { get; }
    [JsonIgnore]
    public string TableName { get; }
    [JsonIgnore]
    public LifeCycleStatus LifeCycleStatus { get; }

    public NormalOperatingStatus NormalOperatingStatus { get; }
    [JsonIgnore]
    public Element Element { get; }

    // ✅ 存“代码 + 名称”，而不是 SDK 的 AssetGroup/AssetType 对象
    public int? AssetGroupCode { get; }
    public string AssetGroupName { get; }
    public int? AssetTypeCode { get; }
    public string AssetTypeName { get; }
    [JsonIgnore]
    public bool IsContainedBySupportStructure { get; set; } = false;
    [JsonIgnore]
    public AssociationStatus AssociationStatus { get; }

    [JsonIgnore]
    public bool IsLVFuse
    {
        get
        {
            //ASSETGROUP = 62 And ASSETTYPE = 672
            return this.AssetGroupName == "LV Fuse" && this.AssetTypeName == "Fuse";
        }
    }
    [JsonIgnore]
    public bool IsLVSourceFuse
    {
        get {
            //ASSETGROUP = 62 And ASSETTYPE = 671
            return this.AssetGroupName == "LV Fuse" && this.AssetTypeName == "Source Fuse";
        }
    }
    [JsonIgnore]
    public bool IsLvLine
    {
        get
        {
            //ASSETGROUP = 62 And ASSETTYPE = 671
            return this.AssetGroupName == "LV Line";
        }
    }


    [JsonIgnore]
    public bool IsLocalSupply
    {
        get
        {
            return this.AssetGroupName == "LV Service Point" && this.AssetTypeName == "Local Supply";
        }
    }

    [JsonIgnore]

    public bool IsLVBusbar
    {
        get
        {
            return this.AssetGroupName == "LV Line" && this.AssetTypeName == "Busbar";
        }
    }

    [JsonIgnore]

    public bool IsHVBusbar
    {
        get
        {
            return this.AssetGroupName == "HV Line" && this.AssetTypeName == "Busbar";
        }
    }

    [JsonIgnore]
    public bool IsLvPillar
    {
        get
        {
            return this.AssetGroupCode == 42 && this.AssetTypeCode == 567;
        }
    }
    [JsonIgnore]
    public bool IsSupplyPoint
    {
        get {
            //ASSETGROUP = 64 And ASSETTYPE = 680
            return this.AssetGroupName == "LV Service Point" && this.AssetTypeName == "Supply Point";
        }
    }
    [JsonIgnore]
    public bool IsLVSwitchingAssembly
    {
        get
        {
            //ASSETGROUP = 42
            return this.AssetGroupCode == 42;
        }
    }
    [JsonIgnore]
    public bool IsLVSwitch
    {
        get {
            //ASSETGROUP = 60 And ASSETTYPE = 650
            return this.AssetGroupName == "LV Switch" && this.AssetTypeName == "Switch";
        }
    }
    [JsonIgnore]
    public bool IsSupportStructure
    {
        get
        {
            return this.AssetGroupCode == 90;
        }
    }

    [JsonIgnore]
    public bool IsLVSwitchingAssemblyLinkBox
    {
        get
        {
            return this.AssetGroupCode == 42 && this.AssetTypeCode == 566;
        }
    }
    [JsonIgnore]
    public bool IsLVSwitchingAssemblyBoard
    {
        get
        {
            return this.AssetGroupCode == 42 && this.AssetTypeCode == 572;
        }
    }
    [JsonIgnore]
    public bool IsLVBoard
    {
        get
        {
            return this.AssetGroupName == "LV Switching Assembly" && this.AssetTypeName == "LV Board";
        }
    }
     

    public Dictionary<string, object> Attributes { get; }
    [JsonIgnore]
    public Geometry Geometry { get; }   // 如 Options.IncludeGeometry=false，则为 null
   

    public FeatureSnapshot(Element element,NetworkSource networkSource, string tableName,
        IDictionary<string, object> attributes, Geometry geometry)
    {
        this.Element = element;
        GlobalID = this.Element.GlobalID;
        ObjectID = this.Element.ObjectID;
        NetworkSourceName = networkSource.Name;
        TableName = tableName;
        Attributes = new Dictionary<string, object>(attributes ?? new Dictionary<string, object>(),
           StringComparer.OrdinalIgnoreCase);
        Geometry = geometry;
        this.AssetGroupCode = this.Element.AssetGroup.Code;
        this.AssetTypeCode = this.Element.AssetType.Code;
        this.AssetGroupName = this.Element.AssetGroup.Name;
        this.AssetTypeName = this.Element.AssetType.Name;
        this.LifeCycleStatus = (LifeCycleStatus)GetValue<short>("LIFECYCLESTATUS", 0);
        this.NormalOperatingStatus = (NormalOperatingStatus)GetValue<short>("NORMALOPERATINGSTATUS", 0);
        this.AssociationStatus = (AssociationStatus)GetValue<short>("ASSOCIATIONSTATUS", 0);
    }

    public string GetString(string field)
    {
        if (!Attributes.TryGetValue(field, out var v) || v == null || v is DBNull) return string.Empty;
        return v.ToString();
    }

    public T GetValue<T>(string field, T @default = default)
    {
        if (!Attributes.TryGetValue(field, out var v) || v == null || v is DBNull) return @default;
        try { return (T)Convert.ChangeType(v, typeof(T)); }
        catch { return @default; }
    }

    public override bool Equals(object obj)
    {
        if (obj is FeatureSnapshot fs) return this.GlobalID == fs.GlobalID;
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return this.GlobalID.GetHashCode();
    }
}

public static class ElementEx
{
    public static bool IsLVFuse(this Element e)
        => e != null && e.AssetGroup.Code == 62 && e.AssetType.Code == 672;

    public static bool IsLVSourceFuse(this Element e)
        => e != null && e.AssetGroup.Code == 62 && e.AssetType.Code == 671;

    public static bool IsSupplyPoint(this Element e)
        => e != null && e.AssetGroup.Code == 64 && e.AssetType.Code == 680;

    public static bool IsLVSwitchingAssembly(this Element e)
        => e != null && e.AssetGroup.Code == 42;




    public static bool IsLVSwitch(this Element e)
        => e != null && e.AssetGroup.Code == 60 && e.AssetType.Code == 658;

    public static bool IsSupportStructure(this Element e)
        => e != null && e.AssetGroup.Code == 90;

    public static bool IsLVSwitchingAssemblyLinkBox(this Element e)
        => e != null && e.AssetGroup.Code == 42 && e.AssetType.Code == 566;

    public static bool IsLVSwitchingAssemblyBoard(this Element e)
        => e != null && e.AssetGroup.Code == 42 && e.AssetType.Code == 572;
}
