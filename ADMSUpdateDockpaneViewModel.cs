using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace CLP.ADMSUpdatePlugin
{
    internal class ADMSUpdateDockpaneViewModel : DockPane
    {
        private const string _dockPaneID = "CLP_ADMSUpdatePlugin_ADMSUpdateDockpane";

        protected ADMSUpdateDockpaneViewModel()
        {
            this.NextStepCommand = new RelayCommand(NextStepAsync, () => this.SelectionElement != null);
            this.BackCommand = new RelayCommand(Back);
            this.UpdateCommand = new RelayCommand(UpdateAsync);

            this.PropertyChanged += ADMSUpdateDockpaneViewModel_PropertyChanged;
        }

        private void ADMSUpdateDockpaneViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "UpdateMode":
                    {
                        switch (this.UpdateMode)
                        {
                            case ADMSUpdateMode.SS_TO_SS:
                                SelectUpdateModeRemark = "Plz select a After Loading BUS ADMS ANAS: (Auto input) Hy Cable/Connector";
                                break;
                            case ADMSUpdateMode.SpareCB:
                                SelectUpdateModeRemark = "Plz select a Circuit breaker feature";
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private string _SelectUpdateModeRemark = "Plz select a After Loading BUS ADMS ANAS: (Auto input) Hy Cable/Connector";

        public string SelectUpdateModeRemark
        {
            get => _SelectUpdateModeRemark;
            set => SetProperty(ref _SelectUpdateModeRemark, value);
        }

        public async Task UpdateAsync()
        {
            await QueuedTask.Run(async () =>
            {
                var un = MapView.Active?.Map
                    .GetLayersAsFlattenedList()
                    .OfType<UtilityNetworkLayer>()
                    .FirstOrDefault()?.GetUtilityNetwork();

                if (un == null)
                {
                    LoggerHelper.Error("Utility Network is not found.");
                    return;
                }

                LoggerHelper.Info("Starting ADMS Name & Alias update process.");

                EditOperation editOp = new EditOperation();
                Inspector insp = new Inspector();

                if (this.FirstHVSwitch != null && this.SecondHVSwitch != null)
                {
                    try
                    {
                        var cbTable = un.GetTable(this.FirstHVSwitch.Source.Element.NetworkSource);

                        if (this.FirstHVSwitch.IsChecked)
                        {
                            // Update ADMS Name & Alias for the first HV Switch
                            insp.Load(cbTable, this.FirstHVSwitch.Source.ObjectID);
                            string firstHVSwitchName = this.FirstHVSwitch.ADMSName;
                            string firstHVSwitchAlias = this.FirstHVSwitch.ADMSAlias;
                            string firstHVSwitchAssetGroup = this.FirstHVSwitch.Source.AssetGroupName;
                            string firstHVSwitchAssetType = this.FirstHVSwitch.Source.AssetTypeName;

                            LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for First HV Switch (ObjectID: {this.FirstHVSwitch.Source.ObjectID}, AssetGroup: {firstHVSwitchAssetGroup}, AssetType: {firstHVSwitchAssetType})");
                            LoggerHelper.Info($"ADMS_Name: {firstHVSwitchName}, ADMS_Alias: {firstHVSwitchAlias}");

                            //SOM_SS = part1
                            //SOM_CCT =

                            insp["ADMS_Name"] = firstHVSwitchName;
                            insp["ADMS_Alias"] = firstHVSwitchAlias;

                            if (this.FirstHVSwitch.Source.AssetGroupName == "HV Switch")
                            {
                                insp["SOM_SS"] = ADMSUpdateHelper.GetCB_SOM_SS(this.FirstHVSwitch);
                                insp["SOM_CCT"] = ADMSUpdateHelper.GetCB_SOM_CCT(this.FirstHVSwitch, this.SecondHVSwitch);
                            }

                            editOp.Modify(insp);


                            if (this.FirstHVSwitch.Busbar != null)
                            {
                                var busTable = un.GetTable(this.FirstHVSwitch.Busbar.Element.NetworkSource);
                                // Update ADMS Name & Alias for the first HV Switch Busbar
                                insp = new Inspector();
                                insp.Load(busTable, this.FirstHVSwitch.Busbar.ObjectID);
                                string firstBusbarName = this.FirstHVSwitch.BusADMSName;
                                string firstBusbarAlias = this.FirstHVSwitch.BusADMSAlias;
                                string firstBusbarAssetGroup = this.FirstHVSwitch.Busbar.AssetGroupName;
                                string firstBusbarAssetType = this.FirstHVSwitch.Busbar.AssetTypeName;

                                LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for First HV Switch Busbar (ObjectID: {this.FirstHVSwitch.Busbar.ObjectID}, AssetGroup: {firstBusbarAssetGroup}, AssetType: {firstBusbarAssetType})");
                                LoggerHelper.Info($"ADMS_Name: {firstBusbarName}, ADMS_Alias: {firstBusbarAlias}");

                                insp["ADMS_Name"] = firstBusbarName;
                                insp["ADMS_Alias"] = firstBusbarAlias;
                                editOp.Modify(insp);
                            }
                        }
                        if (this.SecondHVSwitch.IsChecked)
                        {
                            // Update ADMS Name & Alias for the second HV Switch
                            insp = new Inspector();
                            insp.Load(cbTable, this.SecondHVSwitch.Source.ObjectID);
                            string secondHVSwitchName = this.SecondHVSwitch.ADMSName;
                            string secondHVSwitchAlias = this.SecondHVSwitch.ADMSAlias;
                            string secondHVSwitchAssetGroup = this.SecondHVSwitch.Source.AssetGroupName;
                            string secondHVSwitchAssetType = this.SecondHVSwitch.Source.AssetTypeName;

                            LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for Second HV Switch (ObjectID: {this.SecondHVSwitch.Source.ObjectID}, AssetGroup: {secondHVSwitchAssetGroup}, AssetType: {secondHVSwitchAssetType})");
                            LoggerHelper.Info($"ADMS_Name: {secondHVSwitchName}, ADMS_Alias: {secondHVSwitchAlias}");

                            insp["ADMS_Name"] = secondHVSwitchName;
                            insp["ADMS_Alias"] = secondHVSwitchAlias;

                            if (this.SecondHVSwitch.Source.AssetGroupName == "HV Switch")
                            {
                                insp["SOM_SS"] = ADMSUpdateHelper.GetCB_SOM_SS(this.SecondHVSwitch);
                                insp["SOM_CCT"] = ADMSUpdateHelper.GetCB_SOM_CCT(this.SecondHVSwitch, this.FirstHVSwitch);
                            }
                            editOp.Modify(insp);

                            if (SecondHVSwitch.Busbar != null)
                            {
                                var busTable = un.GetTable(this.SecondHVSwitch.Busbar.Element.NetworkSource);
                                // Update ADMS Name & Alias for the second HV Switch Busbar
                                insp = new Inspector();
                                insp.Load(busTable, this.SecondHVSwitch.Busbar.ObjectID);
                                string secondBusbarName = this.SecondHVSwitch.BusADMSName;
                                string secondBusbarAlias = this.SecondHVSwitch.BusADMSAlias;
                                string secondBusbarAssetGroup = this.SecondHVSwitch.Busbar.AssetGroupName;
                                string secondBusbarAssetType = this.SecondHVSwitch.Busbar.AssetTypeName;

                                LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for Second HV Switch Busbar (ObjectID: {this.SecondHVSwitch.Busbar.ObjectID}, AssetGroup: {secondBusbarAssetGroup}, AssetType: {secondBusbarAssetType})");
                                LoggerHelper.Info($"ADMS_Name: {secondBusbarName}, ADMS_Alias: {secondBusbarAlias}");

                                insp["ADMS_Name"] = secondBusbarName;
                                insp["ADMS_Alias"] = secondBusbarAlias;
                                editOp.Modify(insp);
                            }

                        }
                        if (UpdteCableADMSEnabled && Cables != null && Cables.Any())
                        {
                            var cableTable = un.GetTable(this.Cables.First().Element.NetworkSource);
                            foreach (var cable in Cables)
                            {
                                insp = new Inspector();
                                insp.Load(cableTable, cable.ObjectID);
                                string cableName = cable.Attributes["ADMS_Name"]?.ToString();
                                string cableAlias = cable.Attributes["ADMS_Alias"]?.ToString();
                                string cableAssetGroup = cable.AssetGroupName;
                                string cableAssetType = cable.AssetTypeName;

                                LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for Cable (ObjectID: {cable.ObjectID}, AssetGroup: {cableAssetGroup}, AssetType: {cableAssetType})");
                                LoggerHelper.Info($"ADMS_Name: {cableName}, ADMS_Alias: {cableAlias}");

                                insp["ADMS_Name"] = cableName;
                                insp["ADMS_Alias"] = cableAlias;
                                editOp.Modify(insp);
                            }
                        }
                        if (!editOp.IsEmpty)
                        { 
                            if (editOp.Execute())
                            {
                                LoggerHelper.Info("ADMS Name & Alias update completed successfully.");
                                MessageBox.Show("Update successfully!");
                            }
                            else
                            {
                                LoggerHelper.Error($"Update failed: {editOp.ErrorMessage}");
                                MessageBox.Show("Update fail: " + editOp.ErrorMessage);
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Error($"Exception occurred during ADMS Name & Alias update: {ex.Message}");
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
                else if (this.SpareHVSwitch != null)
                {
                    try
                    {
                        var cbTable = un.GetTable(this.SpareHVSwitch.Source.Element.NetworkSource);

                        if (this.SpareHVSwitch.IsChecked)
                        {
                            // Update ADMS Name & Alias for the first HV Switch
                            insp.Load(cbTable, this.SpareHVSwitch.Source.ObjectID);
                            string firstHVSwitchName = this.SpareHVSwitch.ADMSName;
                            string firstHVSwitchAlias = this.SpareHVSwitch.ADMSAlias;
                            string firstHVSwitchAssetGroup = this.SpareHVSwitch.Source.AssetGroupName;
                            string firstHVSwitchAssetType = this.SpareHVSwitch.Source.AssetTypeName;

                            LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for Spare HV Switch (ObjectID: {this.SpareHVSwitch.Source.ObjectID}, AssetGroup: {firstHVSwitchAssetGroup}, AssetType: {firstHVSwitchAssetType})");
                            LoggerHelper.Info($"ADMS_Name: {firstHVSwitchName}, ADMS_Alias: {firstHVSwitchAlias}");

                            insp["ADMS_Name"] = firstHVSwitchName;
                            insp["ADMS_Alias"] = firstHVSwitchAlias;

                            if (this.SpareHVSwitch.Source.AssetGroupName == "HV Switch")
                            {
                                insp["SOM_SS"] = ADMSUpdateHelper.GetCB_SOM_SS(this.SpareHVSwitch);
                                insp["SOM_CCT"] = ADMSUpdateHelper.GetSpare_CB_SOM_CCT(this.SpareHVSwitch);
                            }

                            editOp.Modify(insp);
                        }
                        if (!editOp.IsEmpty)
                        {
                            if (editOp.Execute())
                            {
                                LoggerHelper.Info("ADMS Name & Alias update completed successfully.");
                                MessageBox.Show("Update successfully!");
                            }
                            else
                            {
                                LoggerHelper.Error($"Update failed: {editOp.ErrorMessage}");
                                MessageBox.Show("Update fail: " + editOp.ErrorMessage);
                            }

                        }
                    } 
                    catch(Exception ex)
                    {
                        LoggerHelper.Error($"Exception occurred during ADMS Name & Alias update: {ex.Message}");
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
                else
                {
                    LoggerHelper.Error("FirstHVSwitch or SecondHVSwitch is null. Update aborted.");
                    MessageBox.Show("FirstHVSwitch or SecondHVSwitch is null.");
                }
            });
        }

        public void Back() {
            this.ShowUpdatePanel = false;
            this.ShowSpareCBUpdatePanel = false;
            this.ShowSearchPanel = true;
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            MapSelectionChangedEvent.Unsubscribe(OnMapSelectionChanged);
        }

        protected override void OnShow(bool isVisible)
        {
            base.OnShow(isVisible);
            MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged, true);
        }

        private bool _ShowSearchPanel = true;
        private bool _ShowUpdatePanel = false;

        public bool ShowSearchPanel
        {
            get => _ShowSearchPanel;
            set => SetProperty(ref _ShowSearchPanel, value);
        }

        public bool ShowUpdatePanel
        {
            get => _ShowUpdatePanel;
            set => SetProperty(ref _ShowUpdatePanel, value);
        }


        private bool _ShowSpareCBUpdatePanel = false;
        
        public bool ShowSpareCBUpdatePanel
        {
            get => _ShowSpareCBUpdatePanel;
            set => SetProperty(ref _ShowSpareCBUpdatePanel, value);
        }


        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private List<Element> _selectionElements = new List<Element>();
        public List<Element> SelectionElements
        {
            get => _selectionElements;
            set => SetProperty(ref _selectionElements, value);
        }


        public async void OnMapSelectionChanged(MapSelectionChangedEventArgs args)
        {
            await QueuedTask.Run(() => {
                var un = MapView.Active?.Map
                    .GetLayersAsFlattenedList()
                    .OfType<UtilityNetworkLayer>().FirstOrDefault()?.GetUtilityNetwork();
                if (un != null)
                {
                    var mapSelectionDict = args.Selection.ToDictionary();
                    HashSet<Element> selectionElements = new HashSet<Element>();
                    foreach (var mapMemberSelection in mapSelectionDict)
                    {
                        var mapMember = mapMemberSelection.Key;
                        if (mapMember is FeatureLayer fLayer)
                        {
                            if (this.UpdateMode == ADMSUpdateMode.SS_TO_SS)
                            {
                                if (fLayer.GetFeatureClass().GetDefinition().GetShapeType() != GeometryType.Polyline) continue;
                                using (var cursor = fLayer.Search(new QueryFilter() { ObjectIDs = mapMemberSelection.Value }))
                                {
                                    while (cursor.MoveNext())
                                    {
                                        var element = un.CreateElement(cursor.Current);
                                        if (element.AssetGroup.Name == "HV Line" && (element.AssetType.Name == "Connector" || element.AssetType.Name == "Cable"))
                                        {
                                            selectionElements.Add(element);
                                        }
                                    }
                                }
                            }
                            else if (this.UpdateMode == ADMSUpdateMode.SpareCB)
                            {
                                using (var cursor = fLayer.Search(new QueryFilter() { ObjectIDs = mapMemberSelection.Value }))
                                {
                                    while (cursor.MoveNext())
                                    {
                                        var element = un.CreateElement(cursor.Current);
                                        if (element.AssetGroup.Name == "HV Switch" && (element.AssetType.Name == "Circuit Breaker" || element.AssetType.Name == "Source Circuit Breaker"))
                                        {
                                            selectionElements.Add(element);
                                        }
                                    }
                                }

                            }
                        }
                    }
                    this.SelectionElements = selectionElements.ToList();
                    if (this.SelectionElements.Any())
                    {
                        this.SelectionElement = this.SelectionElements.FirstOrDefault();
                    }
                }
                
            });
            
        }
        private Element _selectionElement;
        public Element SelectionElement
        {
            get => _selectionElement;
            set => SetProperty(ref _selectionElement, value);
        }

        public async Task TraceHVSwitchs(SS_TO_SS_Model hvSwitchModel,UtilityNetwork utilityNetwork, UtilityNetworkDefinition utilityNetworkDefinition, DomainNetwork domainNetwork, IEnumerable<Element> startElements)
        {
            if (startElements.Count() == 2)
            {
                startElements.First().Terminal = startElements.First().AssetType.GetTerminalConfiguration().Terminals.FirstOrDefault(p => p.Name == "CB:Bus Side" || p.Name== "Source");
                startElements.Last().Terminal = startElements.Last().AssetType.GetTerminalConfiguration().Terminals.FirstOrDefault(p => p.Name == "CB:Line Side" || p.Name == "Load");
            }
            Tier sourceTier = domainNetwork.GetTier("LV");
            TraceConfiguration cfg = sourceTier.GetTraceConfiguration();
            cfg.Propagators = new List<Propagator>();
            var catSub = utilityNetworkDefinition
                .GetAvailableCategories()
                .FirstOrDefault(c => c.Equals("E:Switch", StringComparison.OrdinalIgnoreCase));
            cfg.Filter.Scope = TraversabilityScope.JunctionsAndEdges;
            if (catSub != null)
                if (catSub != null)
                {
                    var catExpr = new CategoryComparison(CategoryOperator.IsEqual, catSub);
                    var existing = cfg.Traversability.Barriers as ConditionalExpression;
                    cfg.Traversability.Barriers = existing == null ? (Condition)catExpr : new Or(existing, catExpr);
                }
            cfg.Traversability.Barriers = TraceCfgHelpers.RemoveAttrFromBarriers(cfg.Traversability.Barriers, new string[] { "NormalOperatingStatus", "Life Cycle Status" });
            var lifeCycleStatuses = new List<int> { 1 }; // 需要的状态值 0, 1, 3
            foreach (var status in lifeCycleStatuses)
            {
                var lifeCycleStatusAttr = TraceCfgHelpers.FindNetworkAttribute(utilityNetworkDefinition, "LifeCycleStatus", "Life Cycle Status");
                if (lifeCycleStatusAttr != null)
                {
                    var statusExpr = new NetworkAttributeComparison(lifeCycleStatusAttr, Operator.Equal, status);
                    var existing = cfg.Traversability.Barriers as ConditionalExpression;
                    cfg.Traversability.Barriers = existing == null ? (Condition)statusExpr : new Or(existing, statusExpr);
                }
            }
            using (TraceManager traceManager = utilityNetwork.GetTraceManager())
            {
                try
                {
                    TraceArgument traceArgument = new TraceArgument(startElements);
                    traceArgument.Configuration = cfg;
                    Tracer tracer = traceManager.GetTracer<ConnectedTracer>();
                    IReadOnlyList<Result> traceResults = tracer.Trace(traceArgument);
                    var results = new SpatialSubgraphExtractor(utilityNetwork).ExtractFromResults(traceResults);
                    var features = results.FeatureByGlobalId.Values;
                    var busBars = features.Where(p => p.IsHVBusbar);
                    if (busBars.Any())
                    {
                        hvSwitchModel.Busbar = busBars.FirstOrDefault();
                        String traceInfo = $"Trace BusBars INFO\nSWitch:[{startElements.First().ObjectID},{startElements.First().GlobalID}],BusBars :[{String.Join(",", busBars.Select(p => $"{p.ObjectID},{p.GlobalID}"))}]";
                        LoggerHelper.Info(traceInfo);
                    }
                }
                catch (Exception e)
                {
                    String traceInfo = $"Fail to trace busbar\nSWitch:FROM [{startElements.First().ObjectID},{startElements.First().GlobalID}],TO[{startElements.Last().ObjectID},{startElements.Last().GlobalID}]";
                    MessageBox.Show("Fail to trace busbar:" + e.Message);
                    LoggerHelper.Error(e, traceInfo);
                }
            }
        }

        private string _CableADMSName;

        public string CableADMSName
        {
            get {
                return _CableADMSName;
            }
            set {
                SetProperty(ref _CableADMSName, value);
            }
        }

        private bool _UpdteCableADMSEnabled = false;

        public bool UpdteCableADMSEnabled
        {
            get
            {
                return _UpdteCableADMSEnabled;
            }
            set
            {
                SetProperty(ref _UpdteCableADMSEnabled, value);
            }
        }

        private string _CableADMSAlias;

        public string CableADMSAlias
        {
            get
            {
                return _CableADMSAlias;
            }
            set
            {
                SetProperty(ref _CableADMSAlias, value);
            }
        }

        private int _CableTotal;

        public int CableTotal
        {
            get
            {
                return _CableTotal;
            }
            set
            {
                SetProperty(ref _CableTotal, value);
            }
        }

        private (string, string, int) GetCableADMSInfo(IEnumerable<FeatureSnapshot> cables, SS_TO_SS_Model first, SS_TO_SS_Model second) {

            this.CableADMSAlias = string.Empty;
            this.CableADMSName = string.Empty;
            this.CableTotal = 0;
            if (cables.Any())
            {

                HashSet<string> cableADMSNames = new HashSet<string>();
                HashSet<string> cableADMSAliases = new HashSet<string>();

                var cable = cables.FirstOrDefault();
                string cableADMSName = ADMSUpdateHelper.GetCableADMSName(first, second, cable, true);
                string cableADMSAlias = ADMSUpdateHelper.GetCableADMSAlias(first, second, cable, true);
                int cableTotal = cables.Count();

                CableADMSAlias = cableADMSAlias;
                CableADMSName = cableADMSName;
                CableTotal = cableTotal;
                return (cableADMSName, cableADMSAlias, cableTotal);
            }
            return (string.Empty, string.Empty, 0);
        }

        public async Task NextStepAsync()
        {
            await QueuedTask.Run(async () => {
                if (SelectionElement == null) return;
                var utilityNetwork = MapView.Active?.Map
                    .GetLayersAsFlattenedList()
                    .OfType<UtilityNetworkLayer>().FirstOrDefault()?.GetUtilityNetwork();
                using (UtilityNetworkDefinition utilityNetworkDefinition = utilityNetwork.GetDefinition())
                {
                    using (NetworkSource networkSource = utilityNetworkDefinition.GetNetworkSource("ElectricDevice"))
                    {
                        try
                        {
                            if (this.UpdateMode == ADMSUpdateMode.SS_TO_SS)
                            {
                                DomainNetwork domainNetwork = utilityNetworkDefinition.GetDomainNetwork("Electric");
                                Tier sourceTier = domainNetwork.GetTier("LV");
                                TraceConfiguration cfg = sourceTier.GetTraceConfiguration();
                                cfg.Propagators = new List<Propagator>();
                                var catSub = utilityNetworkDefinition
                                .GetAvailableCategories()
                                    .FirstOrDefault(c => c.Equals("E:Switch", StringComparison.OrdinalIgnoreCase));
                                cfg.Filter.Scope = TraversabilityScope.JunctionsAndEdges;
                                if (catSub != null)
                                    if (catSub != null)
                                    {
                                        var catExpr = new CategoryComparison(CategoryOperator.IsEqual, catSub);
                                        var existing = cfg.Traversability.Barriers as ConditionalExpression;
                                        cfg.Traversability.Barriers = existing == null ? (Condition)catExpr : new Or(existing, catExpr);
                                    }

                                // condition_barriers="Category IS_EQUAL_TO SPECIFIC_VALUE E:Switch OR;'Asset group' IS_EQUAL_TO SPECIFIC_VALUE 51 OR;'Life Cycle Status' IS_EQUAL_TO SPECIFIC_VALUE 3 OR;'Life Cycle Status' IS_EQUAL_TO SPECIFIC_VALUE 4 OR;'Life Cycle Status' IS_EQUAL_TO SPECIFIC_VALUE 0 #",
                                cfg.Traversability.Barriers = TraceCfgHelpers.RemoveAttrFromBarriers(cfg.Traversability.Barriers, new string[] { "NormalOperatingStatus", "Life Cycle Status" });
                                var lifeCycleStatuses = new List<int> { 0, 4, 3 }; // 需要的状态值 0, 1, 3
                                foreach (var status in lifeCycleStatuses)
                                {
                                    var lifeCycleStatusAttr = TraceCfgHelpers.FindNetworkAttribute(utilityNetworkDefinition, "LifeCycleStatus", "Life Cycle Status");
                                    if (lifeCycleStatusAttr != null)
                                    {
                                        var statusExpr = new NetworkAttributeComparison(lifeCycleStatusAttr, Operator.Equal, status);
                                        var existing = cfg.Traversability.Barriers as ConditionalExpression;
                                        cfg.Traversability.Barriers = existing == null ? (Condition)statusExpr : new Or(existing, statusExpr);
                                    }
                                }
                                var assetGroupAttr = TraceCfgHelpers.FindNetworkAttribute(utilityNetworkDefinition, "Assetgroup", "Asset group");
                                if (assetGroupAttr != null)
                                {
                                    var assetGroupExpr = new NetworkAttributeComparison(assetGroupAttr, Operator.Equal, 51);
                                    var existing = cfg.Traversability.Barriers as ConditionalExpression;
                                    cfg.Traversability.Barriers = existing == null ? (Condition)assetGroupExpr : new Or(existing, assetGroupExpr);
                                }
                                using (TraceManager traceManager = utilityNetwork.GetTraceManager())
                                {
                                    var startElement = this.SelectionElement;


                                    if (startElement.AssetGroup.Name == "HV Switch")
                                    {
                                        if (startElement.AssetType.Name == "Source Circuit Breaker")
                                        {
                                            var tcfg = startElement.AssetType.GetTerminalConfiguration();
                                            startElement.Terminal = tcfg.Terminals.FirstOrDefault(p => p.Name == "Load");
                                        }
                                        else
                                        {
                                            var tcfg = startElement.AssetType.GetTerminalConfiguration();
                                            startElement.Terminal = tcfg.Terminals.FirstOrDefault(p => p.Name == "CB:Line Side");
                                        }
                                    }

                                    //var tcf = this.SelectionHVLine.AssetType.GetTerminalConfiguration();
                                    //var terminal = tcf.Terminals.FirstOrDefault(p => "LOAD".Equals(p.Name, StringComparison.OrdinalIgnoreCase));
                                    //startElement.Terminal = terminal;
                                    TraceArgument traceArgument = new TraceArgument(new List<Element>() { this.SelectionElement });
                                    traceArgument.Configuration = cfg;
                                    Tracer tracer = traceManager.GetTracer<ConnectedTracer>();
                                    IReadOnlyList<Result> traceResults = tracer.Trace(traceArgument);
                                    var results = new SpatialSubgraphExtractor(utilityNetwork).ExtractFromResults(traceResults);
                                    await HighlightPathOnMapAsync(utilityNetwork, results.FeatureByGlobalId.Values);

                                    var features = results.FeatureByGlobalId.Values;
                                    //HV Switch
                                    var hvSwitchs = features.Where(p => p.AssetGroupName == "HV Switch");
                                    var transfomers = features.Where(p => p.AssetGroupName == "Transformer");

                                    if (hvSwitchs.Count() + transfomers.Count() > 2)
                                    {
                                        MessageBox.Show("The process cannot be completed because there are more than two HV Switches or Transformers selected. Please select exactly two HV Switches or one Transformer and one HV Switch.", "Invalid Selection", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                                        return;
                                    }


                                    SS_TO_SS_ResultType resultType = SS_TO_SS_ResultType.CB_TO_CB;
                                    if (transfomers.Any())
                                    {

                                        resultType = SS_TO_SS_ResultType.CB_TO_TRANSFORMER;
                                    }
                                    var hvSwitchAssociations = utilityNetwork.TraverseAssociations(hvSwitchs.Select(p => p.Element), new TraverseAssociationsDescription(TraversalDirection.Ascending));
                                    SS_TO_SS_Model first = null;
                                    SS_TO_SS_Model second = null;

                                    foreach (var hvSwitchAssociation in hvSwitchAssociations.Associations)
                                    {
                                        if (hvSwitchAssociation.FromElement.AssetGroup.Name == "Substation"
                                        && hvSwitchAssociation.ToElement.AssetGroup.Name == "HV Switch")
                                        {
                                            if (first == null)
                                            {
                                                var firstSwitchFeature = features.FirstOrDefault(p => p.Element.GlobalID == hvSwitchAssociation.ToElement.GlobalID);
                                                var firstSubstation = features.FirstOrDefault(p => p.Element.GlobalID == hvSwitchAssociation.FromElement.GlobalID);
                                                first = new SS_TO_SS_Model(firstSwitchFeature, utilityNetwork);
                                                first.SSCODE = firstSubstation.Attributes["SSNUM"]?.ToString();
                                                first.SSNAME = firstSubstation.Attributes["SSNAME"]?.ToString();
                                                first.Source = firstSwitchFeature;
                                                first.Substation = firstSubstation;
                                            }
                                            else
                                            {
                                                var secondSwitchFeature = features.FirstOrDefault(p => p.Element.GlobalID == hvSwitchAssociation.ToElement.GlobalID);
                                                var secondSubstation = features.FirstOrDefault(p => p.Element.GlobalID == hvSwitchAssociation.FromElement.GlobalID);
                                                second = new SS_TO_SS_Model(secondSwitchFeature, utilityNetwork);
                                                second.SSCODE = secondSubstation.Attributes["SSNUM"]?.ToString();
                                                second.SSNAME = secondSubstation.Attributes["SSNAME"]?.ToString();
                                                second.Source = secondSwitchFeature;
                                                second.Substation = secondSubstation;
                                            }
                                        }
                                        if (hvSwitchAssociation.FromElement.AssetGroup.Name == "Support Structure"
                                        && hvSwitchAssociation.ToElement.AssetGroup.Name == "HV Switch")
                                        {
                                            var supportStructureAssociations = utilityNetwork.GetAssociations(hvSwitchAssociation.FromElement, AssociationType.Containment);
                                            string msg = $"Support Structure Assictions:[{String.Join(",", supportStructureAssociations.Select(p => p.FromElement.AssetGroup.Name))}]";
                                            LoggerHelper.Info(msg);
                                            foreach (var supportStructureAssociation in supportStructureAssociations)
                                            {
                                                if (supportStructureAssociation.FromElement.AssetGroup.Name == "Transformer" || supportStructureAssociation.ToElement.AssetGroup.Name == "Transformer")
                                                {
                                                    var transformerElement = supportStructureAssociation.FromElement.AssetGroup.Name == "Transformer" ?
                                                        supportStructureAssociation.FromElement : supportStructureAssociation.ToElement;
                                                    var transformerAssociations = utilityNetwork.GetAssociations(transformerElement, AssociationType.Containment);
                                                    foreach (var transfomerAssociation in transformerAssociations)
                                                    {
                                                        var secondSwitchFeature = features.FirstOrDefault(p => p.Element.GlobalID == hvSwitchAssociation.ToElement.GlobalID);
                                                        var secondSubstation = features.FirstOrDefault(p => p.Element.GlobalID == hvSwitchAssociation.FromElement.GlobalID);
                                                        second = new SS_TO_SS_Model(secondSwitchFeature, utilityNetwork);
                                                        second.SSCODE = secondSubstation.Attributes["SSNUM"]?.ToString();
                                                        second.SSNAME = secondSubstation.Attributes["SSNAME"]?.ToString();
                                                        second.Source = secondSwitchFeature;
                                                        second.Substation = secondSubstation;
                                                    }
                                                }
                                            }
                                            //var supportStructureAssociations = utilityNetwork.TraverseAssociations(new Element[] { hvSwitchAssociation.FromElement }, new TraverseAssociationsDescription(TraversalDirection.Ascending));
                                        }
                                    }
                                    if ((resultType == SS_TO_SS_ResultType.CB_TO_CB || resultType == SS_TO_SS_ResultType.CB_TO_SCB) && first != null && second != null)
                                    {
                                        if (String.IsNullOrEmpty(first.SSNAME) || String.IsNullOrEmpty(first.SSCODE) || String.IsNullOrEmpty(second.SSCODE) || String.IsNullOrEmpty(second.SSNAME))
                                        {
                                            // Log the error for missing substation or switch information
                                            string errorMessage = "Missing information: ";
                                            if (String.IsNullOrEmpty(first.SSNAME))
                                                errorMessage += $"First HV Switch:([{first.Source.ObjectID},{first.Source.GlobalID}]) SSNAME is missing. ";
                                            if (String.IsNullOrEmpty(first.SSCODE))
                                                errorMessage += $"First HV Switch([{first.Source.ObjectID},{first.Source.GlobalID}]) SSCODE is missing. ";
                                            if (String.IsNullOrEmpty(second.SSNAME))
                                                errorMessage += $"Second HV Switch:([{second.Source.ObjectID},{second.Source.GlobalID}]) SSNAME is missing. ";
                                            if (String.IsNullOrEmpty(second.SSCODE))
                                                errorMessage += $"Second HV Switch:([{second.Source.ObjectID},{second.Source.GlobalID}]) SSCODE is missing. ";

                                            // Log the error message
                                            LoggerHelper.Error(errorMessage);

                                            // Show a message box with a detailed error
                                            MessageBox.Show("Cannot proceed: " + errorMessage);
                                            return;
                                        }

                                        first.Target = second;
                                        second.Target = first;
                                        if (first.Source.AssetTypeName != "Source Circuit Breaker" || second.Source.AssetTypeName != "Source Circuit Breaker")
                                        {
                                            second.ResultType = resultType;
                                            first.ResultType = resultType;
                                            String traceInfo = $"Trace INFO:Switch:[{first.Source.ObjectID},{first.Source.GlobalID}],Substation :[{second.SSCODE},{second.SSNAME}],Switch:[{second.Source.ObjectID},{second.Source.GlobalID}],Substation :[{second.SSCODE},{second.SSNAME}]";
                                            LoggerHelper.Info(traceInfo);
                                            await TraceHVSwitchs(first, utilityNetwork, utilityNetworkDefinition, domainNetwork, new Element[] {
                                                first.Source.Element,
                                                second.Source.Element
                                            });
                                            await TraceHVSwitchs(second, utilityNetwork, utilityNetworkDefinition, domainNetwork, new Element[] {
                                                second.Source.Element,
                                                first.Source.Element
                                            });
                                        }
                                        else
                                        {
                                            resultType = SS_TO_SS_ResultType.CB_TO_SCB;
                                            second.ResultType = resultType;
                                            first.ResultType = resultType;
                                        }
                                        // LoggerHelper.Info($"Trace result for:{Tr}");
                                        this.FirstHVSwitch = first;
                                        this.SecondHVSwitch = second;

                                        this.Cables = features.Where(p => p.AssetGroupName == "HV Line" && p.AssetTypeName == "Cable");
                                        foreach (var cable in Cables)
                                        {
                                            cable.Attributes["ADMS_Name"] = ADMSUpdateHelper.GetCableADMSName(first, second, cable);
                                            cable.Attributes["ADMS_Alias"] = ADMSUpdateHelper.GetCableADMSAlias(first, second, cable);
                                        }
                                        GetCableADMSInfo(Cables, first, second);
                                        this.ShowSearchPanel = false;
                                        this.ShowUpdatePanel = true;
                                    }
                                    else if (resultType == SS_TO_SS_ResultType.CB_TO_TRANSFORMER && transfomers.Any())
                                    {
                                        var transfomer = transfomers.First();
                                        var transfomerAssociations = utilityNetwork.TraverseAssociations(transfomers.Select(p => p.Element), new TraverseAssociationsDescription(TraversalDirection.Ascending));
                                        foreach (var transfomerhAssociation in transfomerAssociations.Associations)
                                        {
                                            if (transfomerhAssociation.FromElement.AssetGroup.Name == "Substation"
                                       && transfomerhAssociation.ToElement.AssetGroup.Name == "Transformer")
                                            {
                                                var substation = new SpatialSubgraphExtractor(utilityNetwork).Extract(new Element[] { transfomerhAssociation.FromElement }).FeatureByGlobalId.Values.FirstOrDefault();
                                                second = new SS_TO_SS_Model(transfomer, utilityNetwork);
                                                second.SSCODE = substation.Attributes["SSNUM"]?.ToString();
                                                second.SSNAME = substation.Attributes["SSNAME"]?.ToString();
                                                second.Substation = substation;
                                            }
                                        }
                                        if (String.IsNullOrEmpty(second.SSCODE))
                                        {
                                            second.SSCODE = second.Source.Attributes["SS_CODE"]?.ToString();

                                        }
                                        if (String.IsNullOrEmpty(second.SSNAME))
                                        {
                                            second.SSNAME = second.Source.Attributes["SS_NAME"]?.ToString();
                                        }
                                        second.ResultType = resultType;
                                        first.ResultType = resultType;
                                        first.Target = second;
                                        second.Target = first;
                                        this.FirstHVSwitch = first;
                                        this.SecondHVSwitch = second;
                                        this.Cables = features.Where(p => p.AssetGroupName == "HV Line" && p.AssetTypeName == "Cable");
                                        foreach (var cable in Cables)
                                        {
                                            cable.Attributes["ADMS_Name"] = ADMSUpdateHelper.GetCableADMSName(first, second, cable);
                                            cable.Attributes["ADMS_Alias"] = ADMSUpdateHelper.GetCableADMSAlias(first, second, cable);
                                        }
                                        foreach (var cable in Cables)
                                        {
                                            cable.Attributes["ADMS_Name"] = ADMSUpdateHelper.GetCableADMSName(first, second, cable);
                                            cable.Attributes["ADMS_Alias"] = ADMSUpdateHelper.GetCableADMSAlias(first, second, cable);
                                        }
                                        GetCableADMSInfo(Cables, first, second);
                                        this.ShowSearchPanel = false;
                                        this.ShowUpdatePanel = true;
                                    }
                                    else
                                    {
                                        if (second == null)
                                        {
                                            MessageBox.Show($"Cannot find valid HV Switches in {this.UpdateModels[this.UpdateMode]} mode");
                                        }

                                    }
                                }
                            }
                            else if (this.UpdateMode == ADMSUpdateMode.SpareCB)
                            {
                                var startElement = this.SelectionElement;
                                var hvSwitchAssociations = utilityNetwork.GetAssociations(startElement);
                                SS_TO_SS_Model first = null;
                                if (hvSwitchAssociations.Count() == 0)
                                {
                                    MessageBox.Show("No accociation in CB.");
                                    return;
                                }
                                foreach (var hvSwitchAssociation in hvSwitchAssociations)
                                {
                                    if (hvSwitchAssociation.FromElement.AssetGroup.Name == "Substation" && hvSwitchAssociation.ToElement.AssetGroup.Name == "HV Switch")
                                    {
                                        var deviceLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(l => l.Name == "HV Switch");
                                        var substationLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(l => l.Name == "Substation");
                                        if (deviceLayer == null)
                                        {
                                            MessageBox.Show("Fail to found layer HV Switch.");
                                            return;
                                        }
                                        if (substationLayer == null)
                                        {
                                            MessageBox.Show("Fail to found layer Substation.");
                                            return;
                                        }
                                        deviceLayer.GetFeatureClass().Search();
                                        // Now you can run selections, queries, etc.
                                        FeatureSnapshot firstSwitchFeature = null;
                                        FeatureSnapshot firstSubstationFeature = null;
                                        var qf = new QueryFilter { WhereClause = "GLOBALID = '{" + hvSwitchAssociation.ToElement.GlobalID + "}'" };
                                        using (var switchCursor = deviceLayer.GetFeatureClass().Search(qf))
                                        {
                                            if (switchCursor.MoveNext())
                                            {
                                                var row = switchCursor.Current;
                                                var element = utilityNetwork.CreateElement(row);
                                                var results = new SpatialSubgraphExtractor(utilityNetwork).Extract([element]);
                                                var features = results.FeatureByGlobalId.Values;
                                                firstSwitchFeature = features.FirstOrDefault(p => p.Element.GlobalID == hvSwitchAssociation.ToElement.GlobalID);
                                            }
                                        }

                                        qf.WhereClause = "GLOBALID = '{" + hvSwitchAssociation.FromElement.GlobalID + "}'";
                                        using (var substationCusor = substationLayer.GetFeatureClass().Search(qf))
                                        {

                                            if (substationCusor.MoveNext())
                                            {
                                                var row = substationCusor.Current;
                                                var element = utilityNetwork.CreateElement(row);
                                                var results = new SpatialSubgraphExtractor(utilityNetwork).Extract([element]);
                                                var features = results.FeatureByGlobalId.Values;
                                                firstSubstationFeature = features.FirstOrDefault(p => p.Element.GlobalID == hvSwitchAssociation.FromElement.GlobalID);
                                            }
                                        }

                                        first = new SS_TO_SS_Model(firstSwitchFeature, utilityNetwork);
                                        first.SSCODE = firstSubstationFeature.Attributes["SSNUM"]?.ToString();
                                        first.SSNAME = firstSubstationFeature.Attributes["SSNAME"]?.ToString();
                                        first.Source = firstSwitchFeature;
                                        first.Substation = firstSubstationFeature;
                                        this.SpareHVSwitch = first;
                                        this.ShowSearchPanel = false;
                                        this.ShowSpareCBUpdatePanel = true;
                                    }
                                }
                                if (first == null)
                                {
                                    MessageBox.Show("No accociation between CB and Substation.");
                                    return;
                                }
                                    
                            }

                        }
                        catch (Exception e)
                        {
                            LoggerHelper.Error(e, $"Fail to trace:{SelectionElement.GlobalID}");
                            MessageBox.Show(e.Message);
                        }
                    }
                }
            });
        }

        // GetCableADMSName method (using srcSubstation, desSubstation, cable)

        public IEnumerable<FeatureSnapshot> Cables { get; set; }
        private SS_TO_SS_Model _firstHVSwitch;
        public SS_TO_SS_Model _secondHVSwitch;
        private SS_TO_SS_Model _spareHVSwitch;

        public SS_TO_SS_Model FirstHVSwitch
        {
            get => _firstHVSwitch;
            set => SetProperty(ref _firstHVSwitch, value);
        }


        public SS_TO_SS_Model SecondHVSwitch
        {
            get => _secondHVSwitch;
            set => SetProperty(ref _secondHVSwitch, value);
        }

        public SS_TO_SS_Model SpareHVSwitch
        {
            get => _spareHVSwitch;
            set => SetProperty(ref _spareHVSwitch, value);
        }


        private async Task HighlightPathOnMapAsync(UtilityNetwork un, IEnumerable<FeatureSnapshot> nodes)
        {
            await QueuedTask.Run(() =>
            {
                MapView.Active?.Map.ClearSelection();
                Dictionary<MapMember, IList<long>> selectionMembers = new Dictionary<MapMember, IList<long>>();
                IEnumerable<IGrouping<string, FeatureSnapshot>> byNs = nodes
                       .Where(n => n?.GlobalID != Guid.Empty && !string.IsNullOrEmpty(n.NetworkSourceName))
                       .GroupBy(n => n.NetworkSourceName);
                var unDef = un.GetDefinition();
                var extentList = new List<Geometry>();
                foreach (var g in byNs)
                {
                    NetworkSource ns= unDef.GetNetworkSource(g.Key);
                    using var table = un.GetTable(ns);
                    // 找到对应图层
                    var layers = MapView.Active.Map
                        .GetLayersAsFlattenedList()
                        .OfType<FeatureLayer>()
                        .Where(l => string.Equals((l == null) ? l.GetTable().GetName():"", table.GetName(), StringComparison.OrdinalIgnoreCase));
                    foreach (var layer in layers)
                    {
                        QueryFilter queryFilter = new QueryFilter() { };
                        if (layer.IsSubtypeLayer)
                        {
                            // = oidList
                            queryFilter.WhereClause = $"ASSETGROUP={layer.SubtypeValue} AND ObjectID IN({String.Join(",", g.Select(p => p.ObjectID))})";
                        }
                        else
                        {
                            queryFilter.ObjectIDs = g.Select(p => p.ObjectID).ToArray();
                        }
                        var sel = layer.Select(queryFilter);
                        var objectIDs = sel.GetObjectIDs();
                        if (objectIDs.Any())
                        {
                            selectionMembers.Add(layer, objectIDs.ToArray());
                        }
                    }
                }
                var selection = SelectionSet.FromDictionary(selectionMembers);
                MapView.Active?.Map.SetSelection(selection, SelectionCombinationMethod.New);
                if (extentList.Count > 0)
                {
                    var geometry = GetGeometry(extentList);
                    MapView.Active?.ZoomTo(geometry);
                }
            });
        }

        private Geometry GetGeometry(IEnumerable<Geometry> geometries)
        {
            List<MapPoint> mapPoints = new List<MapPoint>();
            foreach (var geometry in geometries)
            {
                if (geometry.GeometryType == GeometryType.Point)
                {
                    mapPoints.Add(geometry as MapPoint);
                }
                else
                {
                    var points = geometry.GetType().GetProperty("Points")?.GetValue(geometry);
                    if (points != null)
                    {
                        mapPoints.AddRange(points as IEnumerable<MapPoint>);
                    }
                }
            }
            return MultipointBuilderEx.CreateMultipoint(mapPoints.ToHashSet());
        }



        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "Update ADMS name and alias";
        public string Heading
        {
            get => _heading;
            set => SetProperty(ref _heading, value);
        }


        private ADMSUpdateMode _updateMode = ADMSUpdateMode.SS_TO_SS;


        public ADMSUpdateMode UpdateMode
        {
            get => _updateMode;
            set => SetProperty(ref _updateMode, value);
        }

        private Dictionary<ADMSUpdateMode, string> _updateModels = new Dictionary<ADMSUpdateMode, string>() {
            {ADMSUpdateMode.SS_TO_SS, "Update SS To SS" },
            {ADMSUpdateMode.SpareCB, "Update Spare CB" },
        };


        public Dictionary<ADMSUpdateMode, string> UpdateModels
        {
            get
            {
                return _updateModels;
            }
        }
        //NextStepCommand
        public RelayCommand NextStepCommand { get; }

        public RelayCommand BackCommand { get; }

        public RelayCommand UpdateCommand { get; }
    }

    public enum ADMSUpdateMode
    {
        SS_TO_SS,
        SpareCB
    }


    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class ADMSUpdateDockpane_ShowButton : Button
    {
        protected override void OnClick()
        {
            ADMSUpdateDockpaneViewModel.Show();
        }
    }
}