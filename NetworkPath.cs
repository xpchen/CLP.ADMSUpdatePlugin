using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 一条“起点→目标”的数据路径（基于 FeatureSnapshot.Geometry）
/// </summary>
public sealed class NetworkPath
{
    public string PathId { get; set; } = Guid.NewGuid().ToString();

    public FeatureSnapshot Start => Nodes?.FirstOrDefault();
    public FeatureSnapshot End => Nodes?.LastOrDefault();

    /// <summary>路径上的所有节点（含起点与终点），顺序：起点→终点</summary>
    public IList<FeatureSnapshot> Nodes { get; set; } = Array.Empty<FeatureSnapshot>();

    /// <summary>路径长度（边数）</summary>
    public int HopCount => Math.Max(0, (Nodes?.Count ?? 0) - 1);

    /// <summary>中间节点（不含首末）</summary>
    public IEnumerable<FeatureSnapshot> InnerNodes =>
        (Nodes == null || Nodes.Count < 3) ? Array.Empty<FeatureSnapshot>()
                                           : Nodes.Skip(1).Take(Nodes.Count - 2);

    /// <summary>终点是否叶子（构建时填充）</summary>
    public bool EndIsLeaf { get; init; }

    public override string ToString()
        => $"{Start?.GlobalID} -> {End?.GlobalID} (nodes={Nodes?.Count ?? 0}, hops={HopCount})";
}

/// <summary>
/// 构建结果（路径 + 叶子 + 环信息 + 边端点对照，用于 TracePath 端子判断）
/// </summary>
public sealed class PathBuildResult
{
    public List<NetworkPath> Paths { get; init; } = new();
    public List<FeatureSnapshot> LeafCandidates { get; init; } = new();
    public bool HasCycle { get; init; }
    public List<(Guid A, Guid B)> CycleEdges { get; init; } = new();
    public HashSet<Guid> CycleNodes { get; init; } = new();
    /// <summary>边 GlobalID -> (FromJunction, ToJunction)，来自 Connectivity Export；可为 null。</summary>
    public IReadOnlyDictionary<Guid, (Guid, Guid)> EdgeEndpoints { get; init; }

    /// <summary>Connectivity 邻接（供 LinkBox 两跳补判等）；可为 null。</summary>
    public IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> ConnectivityAdjacency { get; init; }
}
