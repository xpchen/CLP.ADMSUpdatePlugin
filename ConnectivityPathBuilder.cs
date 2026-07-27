using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Desktop.Core;
using Newtonsoft.Json.Linq;

/// <summary>
/// 使用 Trace Export 获取 Connectivity 信息来构建路径
/// Export 方法支持 ResultType.Connectivity，而 Trace 方法不支持
/// </summary>
public static class ConnectivityPathBuilder
{
    public sealed class Options
    {
        /// <summary>是否启用调试日志</summary>
        public bool EnableDebugLog { get; init; } = false;

        /// <summary>日志输出委托</summary>
        public Action<string> Logger { get; init; } = null;

        /// <summary>临时文件目录（默认使用项目目录）</summary>
        public string TempDirectory { get; init; } = null;

        /// <summary>是否在完成后删除临时 JSON 文件</summary>
        public bool DeleteTempFile { get; init; } = true;
    }

    /// <summary>
    /// Connectivity JSON 中的连接记录
    /// 表示: From (Junction) -> Via (Edge) -> To (Junction)
    /// </summary>
    public class ConnectivityRecord
    {
        public int FromNetworkSourceId { get; set; }
        public Guid FromGlobalId { get; set; }
        public long FromObjectId { get; set; }
        public int? FromTerminalId { get; set; }

        public int ViaNetworkSourceId { get; set; }
        public Guid ViaGlobalId { get; set; }
        public long ViaObjectId { get; set; }
        public double? ViaPositionFrom { get; set; }
        public double? ViaPositionTo { get; set; }

        public int ToNetworkSourceId { get; set; }
        public Guid ToGlobalId { get; set; }
        public long ToObjectId { get; set; }
        public int? ToTerminalId { get; set; }
    }

    /// <summary>
    /// 导出并解析 Connectivity，返回邻接表与边端点对照（可复用）
    /// </summary>
    public static (Dictionary<Guid, HashSet<Guid>> adjacency, Dictionary<Guid, (Guid, Guid)> edgeEndpoints) ExportAndBuildAdjacency(
        ConnectedTracer tracer,
        TraceArgument traceArgument,
        Guid startGlobalId,
        Dictionary<Guid, FeatureSnapshot> featureSnapshots,
        Options opt = null)
    {
        var emptyAdj = new Dictionary<Guid, HashSet<Guid>>();
        var emptyEdgeEndpoints = new Dictionary<Guid, (Guid, Guid)>();
        opt ??= new Options();
        void Log(string msg) { if (opt.EnableDebugLog && opt.Logger != null) opt.Logger(msg); }

        // 1. 修改 ResultTypes 为 Connectivity
        traceArgument.ResultTypes = new List<ResultType> { ResultType.Connectivity };

        // 2. 设置 Export 选项
        var exportOptions = new TraceExportOptions
        {
            ServiceSynchronizationType = ServiceSynchronizationType.Synchronous,
            IncludeDomainDescriptions = false
        };

        // 3. 准备临时文件路径
        string tempDir = opt.TempDirectory ?? Project.Current?.HomeFolderPath ?? Path.GetTempPath();
        string jsonPath = Path.Combine(tempDir, $"TraceConnectivity_{startGlobalId:N}_{DateTime.Now:yyyyMMddHHmmss}.json");
        Uri jsonUri = new Uri(jsonPath);

        Log($"Exporting connectivity to: {jsonPath}");

        try
        {
            // 4. 执行 Export
            tracer.Export(jsonUri, traceArgument, exportOptions);

            // 5. 解析 JSON 获取 Connectivity
            if (!File.Exists(jsonPath))
            {
                Log($"Export file not found: {jsonPath}");
                return (emptyAdj, emptyEdgeEndpoints);
            }

            string jsonContent = File.ReadAllText(jsonPath);
            var connectivityRecords = ParseConnectivityJson(jsonContent, opt);

            Log($"Parsed {connectivityRecords.Count} connectivity records");

            if (connectivityRecords.Count == 0)
            {
                Log("No connectivity records found");
                return (emptyAdj, emptyEdgeEndpoints);
            }

            // 5b. 从 Connectivity 构建边端点对照
            var edgeEndpoints = BuildEdgeEndpointsFromConnectivity(connectivityRecords);

            // 6. 从 Connectivity 构建邻接关系
            var adj = BuildAdjacencyFromConnectivity(connectivityRecords, featureSnapshots, opt);
            Log($"Built adjacency with {adj.Count} nodes");

            return (adj, edgeEndpoints);
        }
        catch (Exception ex)
        {
            Log($"Error during connectivity export: {ex.Message}");
            return (emptyAdj, emptyEdgeEndpoints);
        }
        finally
        {
            // 清理临时文件
            if (opt.DeleteTempFile && File.Exists(jsonPath))
            {
                try { File.Delete(jsonPath); }
                catch { /* ignore */ }
            }
        }
    }

    /// <summary>
    /// 从已构建的邻接表构建路径（可复用邻接表处理环路）。
    /// <param name="connectivityAdjacencyToStore">若提供，会写入返回的 PathBuildResult.ConnectivityAdjacency，供报告 LinkBox 两跳补判使用。</param>
    /// </summary>
    public static PathBuildResult BuildFromAdjacency(
        Dictionary<Guid, HashSet<Guid>> adjacency,
        Guid startGlobalId,
        Dictionary<Guid, FeatureSnapshot> featureSnapshots,
        Func<FeatureSnapshot, bool> isTarget = null,
        Options opt = null,
        Dictionary<Guid, HashSet<Guid>> connectivityAdjacencyToStore = null,
        IReadOnlyDictionary<Guid, (Guid, Guid)> edgeEndpoints = null)
    {
        opt ??= new Options();
        return BuildPathsFromAdjacency(adjacency, startGlobalId, featureSnapshots, isTarget, opt, connectivityAdjacencyToStore, edgeEndpoints);
    }

    /// <summary>
    /// 使用 Export 获取 Connectivity 并构建路径（便捷方法，内部调用 ExportAndBuildAdjacency + BuildFromAdjacency）
    /// </summary>
    public static PathBuildResult BuildFromConnectivityExport(
        ConnectedTracer tracer,
        TraceArgument traceArgument,
        Guid startGlobalId,
        Dictionary<Guid, FeatureSnapshot> featureSnapshots,
        Func<FeatureSnapshot, bool> isTarget = null,
        Options opt = null)
    {
        var (adj, edgeEndpoints) = ExportAndBuildAdjacency(tracer, traceArgument, startGlobalId, featureSnapshots, opt);
        if (adj.Count == 0)
            return new PathBuildResult();

        return BuildFromAdjacency(adj, startGlobalId, featureSnapshots, isTarget, opt, connectivityAdjacencyToStore: adj, edgeEndpoints: edgeEndpoints);
    }

    /// <summary>
    /// 解析 Connectivity JSON
    /// </summary>
    private static List<ConnectivityRecord> ParseConnectivityJson(string jsonContent, Options opt)
    {
        void Log(string msg) { if (opt.EnableDebugLog && opt.Logger != null) opt.Logger(msg); }

        var records = new List<ConnectivityRecord>();

        try
        {
            var json = JObject.Parse(jsonContent);

            // Connectivity 数据在 "connectivity" 数组中
            var connectivityArray = json["connectivity"] as JArray;
            if (connectivityArray == null)
            {
                Log("No 'connectivity' array found in JSON");
                return records;
            }

            foreach (var item in connectivityArray)
            {
                try
                {
                    var record = new ConnectivityRecord
                    {
                        FromNetworkSourceId = item["fromNetworkSourceId"]?.Value<int>() ?? 0,
                        FromGlobalId = ParseGuid(item["fromGlobalId"]?.Value<string>()),
                        FromObjectId = item["fromObjectId"]?.Value<long>() ?? 0,
                        FromTerminalId = item["fromTerminalId"]?.Value<int?>(),

                        ViaNetworkSourceId = item["viaNetworkSourceId"]?.Value<int>() ?? 0,
                        ViaGlobalId = ParseGuid(item["viaGlobalId"]?.Value<string>()),
                        ViaObjectId = item["viaObjectId"]?.Value<long>() ?? 0,
                        ViaPositionFrom = item["viaPositionFrom"]?.Value<double?>(),
                        ViaPositionTo = item["viaPositionTo"]?.Value<double?>(),

                        ToNetworkSourceId = item["toNetworkSourceId"]?.Value<int>() ?? 0,
                        ToGlobalId = ParseGuid(item["toGlobalId"]?.Value<string>()),
                        ToObjectId = item["toObjectId"]?.Value<long>() ?? 0,
                        ToTerminalId = item["toTerminalId"]?.Value<int?>()
                    };

                    records.Add(record);
                }
                catch (Exception ex)
                {
                    Log($"Error parsing connectivity record: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Error parsing JSON: {ex.Message}");
        }

        return records;
    }

    private static Guid ParseGuid(string s)
    {
        if (string.IsNullOrEmpty(s)) return Guid.Empty;
        s = s.Trim().Trim('{', '}');
        return Guid.TryParse(s, out var g) ? g : Guid.Empty;
    }

    /// <summary>
    /// 从 Connectivity 记录构建边端点对照：ViaGlobalId -> (FromGlobalId, ToGlobalId)。每边只取第一笔记录。
    /// </summary>
    private static Dictionary<Guid, (Guid, Guid)> BuildEdgeEndpointsFromConnectivity(List<ConnectivityRecord> records)
    {
        var edgeEndpoints = new Dictionary<Guid, (Guid, Guid)>();
        if (records == null) return edgeEndpoints;
        foreach (var r in records)
        {
            if (r.ViaGlobalId == Guid.Empty) continue;
            if (!edgeEndpoints.ContainsKey(r.ViaGlobalId))
                edgeEndpoints[r.ViaGlobalId] = (r.FromGlobalId, r.ToGlobalId);
        }
        return edgeEndpoints;
    }

    /// <summary>
    /// 从 Connectivity 记录构建邻接关系
    /// Connectivity 表示: From (Junction) -> Via (Edge) -> To (Junction)
    /// 邻接关系: From <-> Via, Via <-> To
    /// </summary>
    private static Dictionary<Guid, HashSet<Guid>> BuildAdjacencyFromConnectivity(
        List<ConnectivityRecord> records,
        Dictionary<Guid, FeatureSnapshot> featureSnapshots,
        Options opt)
    {
        void Log(string msg) { if (opt.EnableDebugLog && opt.Logger != null) opt.Logger(msg); }

        // 初始化邻接表 - 包含所有 FeatureSnapshot 的 GID
        var allGids = featureSnapshots?.Keys.ToHashSet() ?? new HashSet<Guid>();

        // 添加所有 connectivity 中出现的 GID
        foreach (var r in records)
        {
            if (r.FromGlobalId != Guid.Empty) allGids.Add(r.FromGlobalId);
            if (r.ViaGlobalId != Guid.Empty) allGids.Add(r.ViaGlobalId);
            if (r.ToGlobalId != Guid.Empty) allGids.Add(r.ToGlobalId);
        }

        var adj = allGids.ToDictionary(id => id, _ => new HashSet<Guid>());

        void AddEdge(Guid a, Guid b)
        {
            if (a == Guid.Empty || b == Guid.Empty || a == b) return;
            if (!adj.ContainsKey(a)) adj[a] = new HashSet<Guid>();
            if (!adj.ContainsKey(b)) adj[b] = new HashSet<Guid>();
            adj[a].Add(b);
            adj[b].Add(a);
        }

        // 从 Connectivity 记录建立邻接
        foreach (var r in records)
        {
            // From <-> Via (Junction <-> Edge)
            AddEdge(r.FromGlobalId, r.ViaGlobalId);
            // Via <-> To (Edge <-> Junction)
            AddEdge(r.ViaGlobalId, r.ToGlobalId);
        }

        int edgeCount = adj.Values.Sum(s => s.Count) / 2;
        Log($"Connectivity adjacency: {adj.Count} nodes, {edgeCount} edges");

        return adj;
    }

    /// <summary>
    /// 从邻接关系构建路径（BFS）
    /// </summary>
    private static PathBuildResult BuildPathsFromAdjacency(
        Dictionary<Guid, HashSet<Guid>> adj,
        Guid startGlobalId,
        Dictionary<Guid, FeatureSnapshot> featureSnapshots,
        Func<FeatureSnapshot, bool> isTarget,
        Options opt,
        Dictionary<Guid, HashSet<Guid>> connectivityAdjacencyToStore = null,
        IReadOnlyDictionary<Guid, (Guid, Guid)> edgeEndpoints = null)
    {
        void Log(string msg) { if (opt.EnableDebugLog && opt.Logger != null) opt.Logger(msg); }

        if (!adj.ContainsKey(startGlobalId))
        {
            Log($"Start {startGlobalId} not in adjacency");
            return new PathBuildResult { EdgeEndpoints = edgeEndpoints };
        }

        // BFS 遍历
        var parent = new Dictionary<Guid, Guid?>();
        var cycleEdges = new List<(Guid A, Guid B)>();
        var seenEdge = new HashSet<(Guid, Guid)>();
        var q = new Queue<Guid>();

        parent[startGlobalId] = null;
        q.Enqueue(startGlobalId);

        while (q.Count > 0)
        {
            var u = q.Dequeue();
            if (!adj.TryGetValue(u, out var nbrs)) continue;

            // HashSet 迭代顺序不确定；等距分叉时会影响 parent 选择与整条路径节点序，进而使 TraceFingerprint 跨次运行不一致。
            foreach (var v in nbrs.OrderBy(x => x, Comparer<Guid>.Default))
            {
                if (!parent.ContainsKey(v))
                {
                    parent[v] = u;
                    q.Enqueue(v);
                }
                else if (parent[u] != v)
                {
                    // 发现环
                    var a = u; var b = v;
                    if (Comparer<Guid>.Default.Compare(a, b) > 0) { var t = a; a = b; b = t; }
                    var key = (a, b);
                    if (seenEdge.Add(key))
                        cycleEdges.Add(key);
                }
            }
        }

        var cycleNodes = new HashSet<Guid>(cycleEdges.SelectMany(e => new[] { e.A, e.B }));
        Log($"BFS: {parent.Count} reachable nodes, {cycleEdges.Count} cycle edges");

        // 找叶子节点（度数为1的点，通常是 Junction；排除邊/線用 IsLVLine 判斷，不依賴 Geometry）
        var leafIds = adj
            .Where(kv => kv.Key != startGlobalId && kv.Value.Count == 1)
            .Select(kv => kv.Key)
            .Where(id => featureSnapshots == null || !featureSnapshots.ContainsKey(id) || !featureSnapshots[id].IsLvLine)
            .ToList();

        var leaves = leafIds
            .Where(id => featureSnapshots != null && featureSnapshots.ContainsKey(id))
            .Select(id => featureSnapshots[id])
            .ToList();

        // 确定目标节点
        IEnumerable<Guid> targetIds;
        if (isTarget != null && featureSnapshots != null)
        {
            targetIds = featureSnapshots.Where(kv => isTarget(kv.Value)).Select(kv => kv.Key);
        }
        else
        {
            // 默认使用叶子节点
            targetIds = leafIds;
        }

        var reachableTargets = targetIds.Where(id => parent.ContainsKey(id)).ToList();
        Log($"Targets: {reachableTargets.Count}");

        // 回溯生成路径
        var paths = new List<NetworkPath>();
        foreach (var gid in reachableTargets)
        {
            var seq = new List<FeatureSnapshot>();
            for (Guid? cur = gid; cur != null; cur = parent[cur.Value])
            {
                if (featureSnapshots != null && featureSnapshots.TryGetValue(cur.Value, out var snap))
                {
                    seq.Add(snap);
                }
            }
            seq.Reverse();

            if (seq.Count > 0)
            {
                paths.Add(new NetworkPath
                {
                    Nodes = seq,
                    EndIsLeaf = leafIds.Contains(gid)
                });
            }
        }

        Log($"Built {paths.Count} paths");

        IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> connectivityAdjacencyReadOnly = null;
        if (connectivityAdjacencyToStore != null)
            connectivityAdjacencyReadOnly = connectivityAdjacencyToStore.ToDictionary(kv => kv.Key, kv => (IReadOnlyCollection<Guid>)kv.Value);

        return new PathBuildResult
        {
            Paths = paths,
            LeafCandidates = leaves,
            HasCycle = cycleEdges.Count > 0,
            CycleEdges = cycleEdges,
            CycleNodes = cycleNodes,
            ConnectivityAdjacency = connectivityAdjacencyReadOnly,
            EdgeEndpoints = edgeEndpoints
        };
    }
}