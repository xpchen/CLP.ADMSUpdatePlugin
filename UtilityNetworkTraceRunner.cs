using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CLP.ADMSUpdatePlugin;

public enum TraceBarrierPreset
{
    HvIsolator,
    HvFuse,
    HvSsToSs,
    HvBusbar,
    LvSourceFuse,
    LvPillarFuse,
    LvMotherSupplyFirst,
    LvLinkBox,
}

public sealed class TraceRunRequest
{
    public string TierName { get; init; } = "HV";
    public string TerminalName { get; init; }
    public TraceBarrierPreset Preset { get; init; }
}

public static class UtilityNetworkTraceRunner
{
    public static TraceConfiguration BuildConfiguration(UtilityNetworkDefinition def, TraceRunRequest request)
    {
        TraceConfiguration cfg = TraceCfgHelpers.CreateTierConfiguration(def, request.TierName);
        ApplyPreset(def, cfg, request.Preset);
        return cfg;
    }

    public static void ApplyTerminal(Element element, string terminalName)
    {
        if (string.IsNullOrEmpty(terminalName)) return;
        var terminalConfiguration = element.AssetType.GetTerminalConfiguration();
        element.Terminal = terminalConfiguration.Terminals.FirstOrDefault(p => p.Name == terminalName);
    }

    public static IReadOnlyList<FeatureSnapshot> RunConnectedTrace(
        UtilityNetwork utilityNetwork,
        UtilityNetworkDefinition utilityNetworkDefinition,
        Element startElement,
        TraceRunRequest request)
    {
        ApplyTerminal(startElement, request.TerminalName);
        TraceConfiguration cfg = BuildConfiguration(utilityNetworkDefinition, request);
        using TraceManager traceManager = utilityNetwork.GetTraceManager();
        TraceArgument traceArgument = new TraceArgument(new List<Element>() { startElement }) { Configuration = cfg };
        Tracer tracer = traceManager.GetTracer<ConnectedTracer>();
        IReadOnlyList<Result> traceResults = tracer.Trace(traceArgument);
        return new SpatialSubgraphExtractor(utilityNetwork).ExtractFromResults(traceResults).FeatureByGlobalId.Values.ToList();
    }

    public static IReadOnlyList<FeatureSnapshot> RunConnectedTrace(
        UtilityNetwork utilityNetwork,
        UtilityNetworkDefinition utilityNetworkDefinition,
        IEnumerable<Element> startElements,
        TraceRunRequest request)
    {
        var elements = startElements.ToList();
        TraceConfiguration cfg = BuildConfiguration(utilityNetworkDefinition, request);
        using TraceManager traceManager = utilityNetwork.GetTraceManager();
        TraceArgument traceArgument = new TraceArgument(elements) { Configuration = cfg };
        Tracer tracer = traceManager.GetTracer<ConnectedTracer>();
        IReadOnlyList<Result> traceResults = tracer.Trace(traceArgument);
        return new SpatialSubgraphExtractor(utilityNetwork).ExtractFromResults(traceResults).FeatureByGlobalId.Values.ToList();
    }

    public static async Task<IReadOnlyList<FeatureSnapshot>> RunConnectedTraceAsync(
        UtilityNetwork utilityNetwork,
        UtilityNetworkDefinition utilityNetworkDefinition,
        Element startElement,
        TraceRunRequest request,
        Func<IEnumerable<FeatureSnapshot>, Task> highlight = null)
    {
        var features = RunConnectedTrace(utilityNetwork, utilityNetworkDefinition, startElement, request);
        if (highlight != null)
        {
            await highlight(features);
        }
        return features;
    }

    public static async Task<IReadOnlyList<FeatureSnapshot>> RunConnectedTraceAsync(
        UtilityNetwork utilityNetwork,
        UtilityNetworkDefinition utilityNetworkDefinition,
        IEnumerable<Element> startElements,
        TraceRunRequest request,
        Func<IEnumerable<FeatureSnapshot>, Task> highlight = null)
    {
        var features = RunConnectedTrace(utilityNetwork, utilityNetworkDefinition, startElements, request);
        if (highlight != null)
        {
            await highlight(features);
        }
        return features;
    }

    public static (IReadOnlyList<FeatureSnapshot> features, PathBuildResult pathResult) ExportConnectedTrace(
        UtilityNetwork utilityNetwork,
        UtilityNetworkDefinition utilityNetworkDefinition,
        Element startElement,
        TraceRunRequest request)
    {
        ApplyTerminal(startElement, request.TerminalName);
        TraceConfiguration cfg = BuildConfiguration(utilityNetworkDefinition, request);
        using TraceManager traceManager = utilityNetwork.GetTraceManager();
        TraceArgument traceArgument = new TraceArgument(new List<Element>() { startElement }) { Configuration = cfg };
        Tracer tracer = traceManager.GetTracer<ConnectedTracer>();
        IReadOnlyList<Result> traceResults = tracer.Trace(traceArgument);
        var features = new SpatialSubgraphExtractor(utilityNetwork).ExtractFromResults(traceResults).FeatureByGlobalId.Values.ToList();

        PathBuildResult pathResult = null;
        try
        {
            var featureSnapshots = features.ToDictionary(f => f.GlobalID, f => f);
            pathResult = ConnectivityPathBuilder.BuildFromConnectivityExport(
                (ConnectedTracer)tracer, traceArgument, startElement.GlobalID, featureSnapshots,
                opt: new ConnectivityPathBuilder.Options
                {
                    EnableDebugLog = true,
                    Logger = msg => LoggerHelper.Info(msg),
                    DeleteTempFile = true
                });
        }
        catch (Exception ex)
        {
            LoggerHelper.Error(ex, "Failed to export Isolator trace connectivity JSON");
        }

        return (features, pathResult);
    }

    public static async Task<(IReadOnlyList<FeatureSnapshot> features, PathBuildResult pathResult)> ExportConnectedTraceAsync(
        UtilityNetwork utilityNetwork,
        UtilityNetworkDefinition utilityNetworkDefinition,
        Element startElement,
        TraceRunRequest request,
        Func<IEnumerable<FeatureSnapshot>, Task> highlight = null)
    {
        var (features, pathResult) = ExportConnectedTrace(utilityNetwork, utilityNetworkDefinition, startElement, request);
        if (highlight != null)
        {
            await highlight(features);
        }
        return (features, pathResult);
    }

    private static void ApplyPreset(UtilityNetworkDefinition def, TraceConfiguration cfg, TraceBarrierPreset preset)
    {
        switch (preset)
        {
            case TraceBarrierPreset.HvIsolator:
                cfg.Traversability.Barriers = null;
                TraceCfgHelpers.AddCategoryBarrier(def, cfg, "E:Switch");
                TraceCfgHelpers.AddAssetGroupBarriers(def, cfg, new[] { 61, 51 });
                TraceCfgHelpers.AddLifeCycleBarriers(def, cfg, new[] { 0, 1, 3, 4 });
                TraceCfgHelpers.AddNetworkAttributeBarrier(def, cfg, Operator.Equal, 550, "AssetType", "Asset Type");
                break;

            case TraceBarrierPreset.HvFuse:
                cfg.Traversability.Barriers = TraceCfgHelpers.RemoveAttrFromBarriers(
                    cfg.Traversability.Barriers,
                    "Life Cycle Status", "Asset Type", "AssetType");
                TraceCfgHelpers.AddLifeCycleBarriers(def, cfg, new[] { 0, 1, 3, 4 });
                TraceCfgHelpers.AddAssetGroupBarriers(def, cfg, new[] { 61, 51 });
                TraceCfgHelpers.AddNetworkAttributeBarrier(def, cfg, Operator.Equal, 550, "AssetType", "Asset Type");
                break;

            case TraceBarrierPreset.HvSsToSs:
                TraceCfgHelpers.AddCategoryBarrier(def, cfg, "E:Switch");
                cfg.Traversability.Barriers = TraceCfgHelpers.RemoveAttrFromBarriers(
                    cfg.Traversability.Barriers,
                    "NormalOperatingStatus", "Life Cycle Status");
                TraceCfgHelpers.AddLifeCycleBarriers(def, cfg, new[] { 0, 4, 3 });
                TraceCfgHelpers.AddAssetGroupBarriers(def, cfg, new[] { 51 });
                break;

            case TraceBarrierPreset.HvBusbar:
                TraceCfgHelpers.AddCategoryBarrier(def, cfg, "E:Switch");
                cfg.Traversability.Barriers = TraceCfgHelpers.RemoveAttrFromBarriers(
                    cfg.Traversability.Barriers,
                    "NormalOperatingStatus", "Life Cycle Status");
                TraceCfgHelpers.AddLifeCycleBarriers(def, cfg, new[] { 0, 1, 3, 4 });
                break;

            case TraceBarrierPreset.LvSourceFuse:
                TraceCfgHelpers.AddCategoryBarrier(def, cfg, "Subnetwork Controller");
                cfg.Traversability.Barriers = TraceCfgHelpers.RemoveAttrFromBarriers(
                    cfg.Traversability.Barriers,
                    "NormalOperatingStatus", "Life Cycle Status");
                TraceCfgHelpers.AddLifeCycleBarriers(def, cfg, new[] { 0, 4, 3 });
                TraceCfgHelpers.AddAssetGroupBarriers(def, cfg, new[] { 51 });
                break;

            case TraceBarrierPreset.LvPillarFuse:
                cfg.Traversability.Barriers = null;
                var catSwitchFuse = def.GetAvailableCategories()
                    .FirstOrDefault(c => c.Equals("E:Switch - Fuse", StringComparison.OrdinalIgnoreCase));
                if (catSwitchFuse != null)
                {
                    cfg.Traversability.Barriers = new CategoryComparison(CategoryOperator.IsEqual, catSwitchFuse);
                }
                break;

            case TraceBarrierPreset.LvMotherSupplyFirst:
                cfg.Traversability.Barriers = null;
                TraceCfgHelpers.AddCategoryBarrier(def, cfg, "Subnetwork Controller");
                TraceCfgHelpers.AddNetworkAttributeBarrier(
                    def, cfg, Operator.Equal, (int)NormalOperatingStatus.Open,
                    "NormalOperatingStatus", "Normal Operating Status");
                TraceCfgHelpers.AddLifeCycleBarriers(def, cfg, new[] { 0, 1, 3, 4 });
                break;

            case TraceBarrierPreset.LvLinkBox:
                cfg.Traversability.Barriers = null;
                var catSwitch = def.GetAvailableCategories()
                    .FirstOrDefault(c => c.Equals("E:Switch", StringComparison.OrdinalIgnoreCase));
                if (catSwitch != null)
                {
                    cfg.Traversability.Barriers = new CategoryComparison(CategoryOperator.IsEqual, catSwitch);
                }
                break;
        }
    }
}
