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

        private string _admsNameDisplay;

        private string _admsAliasDisplay;

        public string ADMSNameDisplay
        {
            get => _admsNameDisplay;
            set => SetProperty(ref _admsNameDisplay, value);
        }

        public string ADMSAliasDisplay
        {
            get => _admsAliasDisplay;
            set => SetProperty(ref _admsAliasDisplay, value);
        }

        // Below value for Pole Cable
        private string _cableCircuitName;

        private string _cableCircuitID;

        public string CABLE_CIRCUIT_NAME
        {
            get => _cableCircuitName;
            set => SetProperty(ref _cableCircuitName, value);
        }

        public string CABLE_CIRCUIT_ID
        {
            get => _cableCircuitID;
            set => SetProperty(ref _cableCircuitID, value);
        }

        protected ADMSUpdateDockpaneViewModel()
        {
            this.NextStepCommand = new RelayCommand(NextStepAsync, () => this.SelectionElement != null || 
                                                                        (this.UpdateMode == ADMSUpdateMode.PoleCable && this.SelectionElements.Count != 0));
            this.BackCommand = new RelayCommand(Back);
            this.UpdateCommand = new RelayCommand(UpdateAsync);
            this.RefreshCommand = new RelayCommand(RefreshADMS);

            this.PropertyChanged += ADMSUpdateDockpaneViewModel_PropertyChanged;
        }

        private void ADMSUpdateDockpaneViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "UpdateMode":
                    {
                        this.SelectionElements = [];
                        switch (this.UpdateMode)
                        {
                            case ADMSUpdateMode.SS_TO_SS:
                                SelectUpdateModeRemark = "Plz select a After Loading BUS ADMS ANAS: (Auto input) By Cable/Connector";
                                break;
                            case ADMSUpdateMode.SpareCB:
                                SelectUpdateModeRemark = "Plz select a Circuit breaker feature";
                                break;
                            case ADMSUpdateMode.Pole:
                                SelectUpdateModeRemark = "Plz select a Pole feature (Isolator/Fuse/Transformer/Switch/Subring Circuit Breaker)";
                                break;
                            case ADMSUpdateMode.PoleCable:
                                SelectUpdateModeRemark = "Plz select the Cable/OHL that needs to be updated";
                                break;
                            case ADMSUpdateMode.LVFeature:
                                SelectUpdateModeRemark = "Plz slsect a LV feature (SourceFuse/LocalSupply/SupplyPoint/PillarFuse/LinkBoxLeg)";
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

                LoggerHelper.Info($"Starting ADMS Name & Alias update process at {DateTime.Now}.");

                EditOperation editOp = new EditOperation();
                Inspector insp = new Inspector();

                if (this.FirstHVSwitch != null && this.SecondHVSwitch != null && this.UpdateMode == ADMSUpdateMode.SS_TO_SS)
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

                            if (this.FirstHVSwitch.BusNodes != null)
                            {
                                var busNodeTable = un.GetTable(this.FirstHVSwitch.BusNodes.FirstOrDefault().Element.NetworkSource);
                                // Update ADMS Name & Alias for BusNodes
                                insp = new Inspector();
                                foreach (FeatureSnapshot busNode in this.FirstHVSwitch.BusNodes)
                                {
                                    insp.Load(busNodeTable, busNode.ObjectID);
                                    string firstBusbarName = this.FirstHVSwitch.BusADMSName;
                                    string firstBusbarAlias = this.FirstHVSwitch.BusADMSAlias;
                                    LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for First HV Switch Busbar (ObjectID: {busNode.ObjectID}, AssetGroup: {busNode.AssetGroupName}, AssetType: {busNode.AssetTypeName})");
                                    LoggerHelper.Info($"ADMS_Name: {firstBusbarName}, ADMS_Alias: {firstBusbarAlias}");

                                    insp["ADMS_Name"] = firstBusbarName;
                                    insp["ADMS_Alias"] = firstBusbarAlias;
                                    editOp.Modify(insp);
                                }
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

                            if (this.SecondHVSwitch.BusNodes != null)
                            {
                                var busNodeTable = un.GetTable(this.SecondHVSwitch.BusNodes.FirstOrDefault().Element.NetworkSource);
                                // Update ADMS Name & Alias for BusNodes
                                insp = new Inspector();
                                foreach (FeatureSnapshot busNode in this.SecondHVSwitch.BusNodes)
                                {
                                    insp.Load(busNodeTable, busNode.ObjectID);
                                    string firstBusbarName = this.SecondHVSwitch.BusADMSName;
                                    string firstBusbarAlias = this.SecondHVSwitch.BusADMSAlias;
                                    LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for First HV Switch Busbar (ObjectID: {busNode.ObjectID}, AssetGroup: {busNode.AssetGroupName}, AssetType: {busNode.AssetTypeName})");
                                    LoggerHelper.Info($"ADMS_Name: {firstBusbarName}, ADMS_Alias: {firstBusbarAlias}");

                                    insp["ADMS_Name"] = firstBusbarName;
                                    insp["ADMS_Alias"] = firstBusbarAlias;
                                    editOp.Modify(insp);
                                }
                            }
                        }
                        if (UpdteCableADMSEnabled && Cables != null && Cables.Any())
                        {
                            var tmpCables = Cables.Where(p => p.AssetTypeName == "Cable");
                            var tmpConnections = this.Cables.Where(p => p.AssetGroupName == "HV Connection Point");
                            if(tmpCables.Any())
                            {
                                var cableTable = un.GetTable(tmpCables.First().Element.NetworkSource);
                                foreach (var cable in tmpCables)
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
                                    insp["terminated_substation"] = ADMSUpdateHelper.GetCable_Terminal_Substation(this.FirstHVSwitch, SecondHVSwitch);
                                    editOp.Modify(insp);
                                }
                            }
                            if (tmpConnections.Any())
                            {
                                var connectionTable = un.GetTable(tmpConnections.First().Element.NetworkSource);
                                foreach (var connection in tmpConnections)
                                {
                                    insp = new Inspector();
                                    insp.Load(connectionTable, connection.ObjectID);
                                    string cableAssetGroup = connection.AssetGroupName;
                                    string cableAssetType = connection.AssetTypeName;
                                    string terminated_substation = ADMSUpdateHelper.GetCable_Terminal_Substation(this.FirstHVSwitch, SecondHVSwitch);

                                    LoggerHelper.Info($"Updating Terminated Substation for Joint/Termination (ObjectID: {connection.ObjectID}, AssetGroup: {cableAssetGroup}, AssetType: {cableAssetType})");
                                    LoggerHelper.Info($"Terminated Substation: {terminated_substation}");

                                    insp["terminated_substation"] = terminated_substation;
                                    editOp.Modify(insp);
                                }
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
                else if (this.SpareHVSwitch != null && this.UpdateMode == ADMSUpdateMode.SpareCB)
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
                else if (this.PoleDevice != null && this.UpdateMode == ADMSUpdateMode.Pole)
                {
                    try
                    {
                        var table = un.GetTable(this.PoleDevice.Source.Element.NetworkSource);

                        // Update ADMS Name & Alias for the first HV Switch
                        insp.Load(table, this.PoleDevice.Source.ObjectID);
                        string firstName = this.PoleDevice.ADMS_Name;
                        string firstAlias = this.PoleDevice.ADMS_Alias;
                        string firstHVSwitchAssetGroup = this.PoleDevice.Source.AssetGroupName;
                        string firstHVSwitchAssetType = this.PoleDevice.Source.AssetTypeName;

                        LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for Spare HV Switch (ObjectID: {this.PoleDevice.Source.ObjectID}, AssetGroup: {firstHVSwitchAssetGroup}, AssetType: {firstHVSwitchAssetType})");
                        LoggerHelper.Info($"ADMS_Name: {firstName}, ADMS_Alias: {firstAlias}");

                        insp["ADMS_Name"] = firstName;
                        insp["ADMS_Alias"] = firstAlias;

                        if (this.PoleDevice.Source.AssetTypeName == "Isolator")
                        {
                            insp["SOM_SS"] = ADMSUpdateHelper.GetIsolator_SOM_SS(this.PoleDevice);
                            insp["SOM_CCT"] = ADMSUpdateHelper.GetIsolator_SOM_CCT(this.PoleDevice);
                        }
                        else if (this.PoleDevice.Source.AssetTypeName == "Switch")
                        {
                            insp["SOM_SS"] = ADMSUpdateHelper.GetPMS_SOM_SS(this.PoleDevice);
                            insp["SOM_CCT"] = ADMSUpdateHelper.GetPMS_SOM_CCT(this.PoleDevice);
                        }
                        else if (this.PoleDevice.Source.AssetTypeName == "Subring Circuit Breaker")
                        {
                            insp["SOM_SS"] = ADMSUpdateHelper.GetSubringCB_SOM_SS(this.PoleDevice);
                            insp["SOM_CCT"] = ADMSUpdateHelper.GetSubringCB_SOM_CCT(this.PoleDevice);
                        }

                        editOp.Modify(insp);
                        
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
                else if (this.UpdateMode == ADMSUpdateMode.PoleCable) // no any big object needed
                {
                    try
                    {
                        foreach (var selectionElement in this.SelectionElements)
                        {
                            var table = un.GetTable(selectionElement.NetworkSource);

                            // Update ADMS Name & Alias for all selected Cable/OHL
                            insp.Load(table, selectionElement.ObjectID);

                            string cableADMSName = ADMSUpdateHelper.GetADMSNameForPoleCable(this.CABLE_CIRCUIT_NAME, $"{selectionElement.ObjectID}");
                            string cableADMSAlias = ADMSUpdateHelper.GetADMSAliasForPoleCable(this.CABLE_CIRCUIT_ID, $"{selectionElement.ObjectID}");

                            LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for Spare HV Switch (ObjectID: {selectionElement.ObjectID}, AssetGroup: {selectionElement.AssetGroup.Name}, AssetType: {selectionElement.AssetType.Name})");
                            LoggerHelper.Info($"ADMS_Name: {cableADMSName}, ADMS_Alias: {cableADMSAlias}");

                            insp["ADMS_Name"] = cableADMSName;
                            insp["ADMS_Alias"] = cableADMSAlias;

                            editOp.Modify(insp);

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
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Error($"Exception occurred during ADMS Name & Alias update: {ex.Message}");
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
                else if (this.LVFeatureContainer != null && this.UpdateMode == ADMSUpdateMode.LVFeature)
                {
                    try
                    {
                        foreach (var sourceFuse in this.LVFeatureContainer.SourceFusesToUpdate)
                        {
                            var table = un.GetTable(sourceFuse.Source.Element.NetworkSource);
                            insp = new Inspector();
                            insp.Load(table, sourceFuse.Source.ObjectID);

                            LoggerHelper.Info($"Updating ADMS_Name, ADMS_Alias, SOM_SS and SOM_CCT for Source Fuse (ObjectID: {sourceFuse.Source.ObjectID}, AssetGroup: {sourceFuse.Source.AssetGroupName}, AssetType: {sourceFuse.Source.AssetTypeName})");
                            LoggerHelper.Info($"ADMS_Name: {sourceFuse.ADMS_Name}, ADMS_Alias: {sourceFuse.ADMS_Alias}, SOM_SS: {sourceFuse.SOMSS}, SOM_CCT: {sourceFuse.SOMCCT}");

                            insp["ADMS_Name"] = sourceFuse.ADMS_Name;
                            insp["ADMS_Alias"] = sourceFuse.ADMS_Alias;
                            insp["SOM_SS"] = sourceFuse.SOMSS;
                            insp["SOM_CCT"] = sourceFuse.SOMCCT;

                            editOp.Modify(insp);
                        }

                        foreach (var localSupplyPoint in this.LVFeatureContainer.UpdateLocalSupplyPoint ? this.LVFeatureContainer.LocalSupplyPoints : new List<LVFeature_Model>())
                        {
                            var table = un.GetTable(localSupplyPoint.Source.Element.NetworkSource);
                            insp = new Inspector();
                            insp.Load(table, localSupplyPoint.Source.ObjectID);

                            LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for Local Supply (ObjectID: {localSupplyPoint.Source.ObjectID}, AssetGroup: {localSupplyPoint.Source.AssetGroupName}, AssetType: {localSupplyPoint.Source.AssetTypeName})");
                            LoggerHelper.Info($"ADMS_Name: {localSupplyPoint.ADMS_Name}, ADMS_Alias: {localSupplyPoint.ADMS_Alias}");

                            insp["ADMS_Name"] = localSupplyPoint.ADMS_Name;
                            insp["ADMS_Alias"] = localSupplyPoint.ADMS_Alias;

                            editOp.Modify(insp);
                        }

                        foreach (var pillarFuse in this.LVFeatureContainer.PillarFusesToUpdate)
                        {
                            var table = un.GetTable(pillarFuse.Source.Element.NetworkSource);
                            insp = new Inspector();
                            insp.Load(table, pillarFuse.Source.ObjectID);

                            LoggerHelper.Info($"Updating ADMS_Name, ADMS_Alias, SOM_SS and SOM_CCT for Pillar Fuse (ObjectID: {pillarFuse.Source.ObjectID}, AssetGroup: {pillarFuse.Source.AssetGroupName}, AssetType: {pillarFuse.Source.AssetTypeName})");
                            LoggerHelper.Info($"ADMS_Name: {pillarFuse.ADMS_Name}, ADMS_Alias: {pillarFuse.ADMS_Alias}, SOM_SS: {pillarFuse.SOMSS}, SOM_CCT: {pillarFuse.SOMCCT}");

                            insp["ADMS_Name"] = pillarFuse.ADMS_Name;
                            insp["ADMS_Alias"] = pillarFuse.ADMS_Alias;
                            insp["SOM_SS"] = pillarFuse.SOMSS;
                            insp["SOM_CCT"] = pillarFuse.SOMCCT;

                            editOp.Modify(insp);
                        }

                        if (this.LVFeatureContainer.UpdatePillarCircuitBox && this.LVFeatureContainer.PillarCircuitBox != null)
                        {
                            var table = un.GetTable(this.LVFeatureContainer.PillarCircuitBox.Source.Element.NetworkSource);
                            insp = new Inspector();
                            insp.Load(table, this.LVFeatureContainer.PillarCircuitBox.Source.ObjectID);

                            LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for Pillar Circuit Box (ObjectID: {this.LVFeatureContainer.PillarCircuitBox.Source.ObjectID}, AssetGroup: {this.LVFeatureContainer.PillarCircuitBox.Source.AssetGroupName}, AssetType: {this.LVFeatureContainer.PillarCircuitBox.Source.AssetTypeName})");
                            LoggerHelper.Info($"ADMS_Name: {this.LVFeatureContainer.PillarCircuitBox.ADMS_Name}, ADMS_Alias: {this.LVFeatureContainer.PillarCircuitBox.ADMS_Alias}");

                            insp["ADMS_Name"] = this.LVFeatureContainer.PillarCircuitBox.ADMS_Name;
                            insp["ADMS_Alias"] = this.LVFeatureContainer.PillarCircuitBox.ADMS_Alias;

                            editOp.Modify(insp);
                        }

                        if (this.LVFeatureContainer.UpdateSupplyPoint && this.LVFeatureContainer.SupplyPoint != null)
                        {
                            var table = un.GetTable(this.LVFeatureContainer.SupplyPoint.Source.Element.NetworkSource);
                            insp = new Inspector();
                            insp.Load(table, this.LVFeatureContainer.SupplyPoint.Source.ObjectID);

                            LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for Supply Point (ObjectID: {this.LVFeatureContainer.SupplyPoint.Source.ObjectID}, AssetGroup: {this.LVFeatureContainer.SupplyPoint.Source.AssetGroupName}, AssetType: {this.LVFeatureContainer.SupplyPoint.Source.AssetTypeName})");
                            LoggerHelper.Info($"ADMS_Name: {this.LVFeatureContainer.SupplyPoint.ADMS_Name}, ADMS_Alias: {this.LVFeatureContainer.SupplyPoint.ADMS_Alias}");

                            insp["ADMS_Name"] = this.LVFeatureContainer.SupplyPoint.ADMS_Name;
                            insp["ADMS_Alias"] = this.LVFeatureContainer.SupplyPoint.ADMS_Alias;

                            editOp.Modify(insp);
                        }

                        if (this.LVFeatureContainer.UpdateLinkBox && this.LVFeatureContainer.LinkBox != null)
                        {
                            var table = un.GetTable(this.LVFeatureContainer.LinkBox.Source.Element.NetworkSource);
                            insp = new Inspector();
                            insp.Load(table, this.LVFeatureContainer.LinkBox.Source.ObjectID);

                            LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for Link Box (ObjectID: {this.LVFeatureContainer.LinkBox.Source.ObjectID}, AssetGroup: {this.LVFeatureContainer.LinkBox.Source.AssetGroupName}, AssetType: {this.LVFeatureContainer.LinkBox.Source.AssetTypeName})");
                            LoggerHelper.Info($"ADMS_Name: {this.LVFeatureContainer.LinkBox.ADMS_Name}, ADMS_Alias: {this.LVFeatureContainer.LinkBox.ADMS_Alias}");

                            insp["ADMS_Name"] = this.LVFeatureContainer.LinkBox.ADMS_Name;
                            insp["ADMS_Alias"] = this.LVFeatureContainer.LinkBox.ADMS_Alias;

                            editOp.Modify(insp);
                        }

                        foreach (var lvSwitch in this.LVFeatureContainer.LVSwitchesToUpdate)
                        {
                            var table = un.GetTable(lvSwitch.Source.Element.NetworkSource);
                            insp = new Inspector();
                            insp.Load(table, lvSwitch.Source.ObjectID);

                            LoggerHelper.Info($"Updating ADMS_Name, ADMS_Alias, SOM_SS and SOM_CCT for LV Switch (ObjectID: {lvSwitch.Source.ObjectID}, AssetGroup: {lvSwitch.Source.AssetGroupName}, AssetType: {lvSwitch.Source.AssetTypeName})");
                            LoggerHelper.Info($"ADMS_Name: {lvSwitch.ADMS_Name}, ADMS_Alias: {lvSwitch.ADMS_Alias}, SOM_SS: {lvSwitch.SOMSS}, SOM_CCT: {lvSwitch.SOMCCT}");

                            insp["ADMS_Name"] = lvSwitch.ADMS_Name;
                            insp["ADMS_Alias"] = lvSwitch.ADMS_Alias;
                            insp["SOM_SS"] = lvSwitch.SOMSS;
                            insp["SOM_CCT"] = lvSwitch.SOMCCT;

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
                    catch (Exception ex)
                    {
                        LoggerHelper.Error($"Exception occurred during ADMS Name & Alias update: {ex.Message}");
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }
                else if (this.LVFeature != null && this.UpdateMode == ADMSUpdateMode.LVFeature)
                {
                    try
                    {
                        var table = un.GetTable(this.LVFeature.Source.Element.NetworkSource);
                        insp = new Inspector();
                        insp.Load(table, this.LVFeature.Source.ObjectID);

                        LoggerHelper.Info($"Updating ADMS_Name and ADMS_Alias for LV Feature (ObjectID: {this.LVFeature.Source.ObjectID}, AssetGroup: {this.LVFeature.Source.AssetGroupName}, AssetType: {this.LVFeature.Source.AssetTypeName})");
                        LoggerHelper.Info($"ADMS_Name: {this.LVFeature.ADMS_Name}, ADMS_Alias: {this.LVFeature.ADMS_Alias}");

                        insp["ADMS_Name"] = this.LVFeature.ADMS_Name;
                        insp["ADMS_Alias"] = this.LVFeature.ADMS_Alias;
                        if (this.LVFeature.IsMotherSupplyPoint)
                        {
                            insp["SOM_SS"] = this.LVFeature.SOMSS;
                            insp["SOM_CCT"] = this.LVFeature.SOMCCT;
                        }

                        editOp.Modify(insp);

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
                else
                {
                    LoggerHelper.Error("FirstHVSwitch or SecondHVSwitch is null. Update aborted.");
                    MessageBox.Show("FirstHVSwitch or SecondHVSwitch is null.");
                }
                LoggerHelper.Info($"Ending ADMS Name & Alias update process at {DateTime.Now}.");
            });
        }

        public void Back() {
            this.FirstHVSwitch = null;
            this.SecondHVSwitch = null;
            this.SpareHVSwitch = null;
            this.PoleDevice = null;
            this.LVFeature = null;
            this.LVFeatureContainer = null;
            this.ShowLVSourceFusePanel = false;
            this.ShowLVSupplyPointPanel = false;
            this.ShowLVPillarFusePanel = false;
            this.ShowLVLinkBoxPanel = false;
            this.ShowLVMotherSupplyPointPanel = false;
            this.ShowUpdatePanel = false;
            this.ShowSpareCBUpdatePanel = false;
            this.ShowPolePanel = false;
            this.ShowPoleCablePanel = false;
            this.ShowLVFeaturePanel = false;
            this.ShowSearchPanel = true;
            this.ADMSAliasDisplay = "";
            this.ADMSNameDisplay = "";
            this.UpdteCableADMSEnabled = false;
        }

        public async Task RefreshADMS()
        {
            await QueuedTask.Run(async () =>
            {
                if (this.UpdateMode == ADMSUpdateMode.Pole)
                {
                    this.ADMSNameDisplay = this.PoleDevice.ADMS_Name;
                    this.ADMSAliasDisplay = this.PoleDevice.ADMS_Alias;
                }
                else if(this.UpdateMode == ADMSUpdateMode.PoleCable)
                {
                    this.ADMSNameDisplay = ADMSUpdateHelper.GetADMSNameForPoleCable(this.CABLE_CIRCUIT_NAME, "XXXXXX");
                    this.ADMSAliasDisplay = ADMSUpdateHelper.GetADMSAliasForPoleCable(this.CABLE_CIRCUIT_ID, "XXXXXX");
                }
                
            });
            
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
        private bool _ShowSpareCBUpdatePanel = false;
        private bool _ShowPolePanel = false;
        private bool _ShowPoleCablePanel = false;
        private bool _ShowLVFeaturePanel = false;
        private bool _ShowLVSourceFusePanel = false;
        private bool _ShowLVSupplyPointPanel = false;
        private bool _ShowLVPillarFusePanel = false;
        private bool _ShowLVLinkBoxPanel = false;
        private bool _ShowLVMotherSupplyPointPanel = false;

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

        public bool ShowSpareCBUpdatePanel
        {
            get => _ShowSpareCBUpdatePanel;
            set => SetProperty(ref _ShowSpareCBUpdatePanel, value);
        }

        public bool ShowPolePanel
        {
            get => _ShowPolePanel;
            set => SetProperty(ref _ShowPolePanel, value);
        }

        public bool ShowPoleCablePanel
        {
            get => _ShowPoleCablePanel;
            set => SetProperty(ref _ShowPoleCablePanel, value);
        }

        public bool ShowLVFeaturePanel
        {
            get => _ShowLVFeaturePanel;
            set => SetProperty(ref _ShowLVFeaturePanel, value);
        }

        public bool ShowLVSourceFusePanel
        {
            get => _ShowLVSourceFusePanel;
            set => SetProperty(ref _ShowLVSourceFusePanel, value);
        }

        public bool ShowLVSupplyPointPanel
        {
            get => _ShowLVSupplyPointPanel;
            set => SetProperty(ref _ShowLVSupplyPointPanel, value);
        }

        public bool ShowLVPillarFusePanel
        {
            get => _ShowLVPillarFusePanel;
            set => SetProperty(ref _ShowLVPillarFusePanel, value);
        }

        public bool ShowLVLinkBoxPanel
        {
            get => _ShowLVLinkBoxPanel;
            set => SetProperty(ref _ShowLVLinkBoxPanel, value);
        }

        public bool ShowLVMotherSupplyPointPanel
        {
            get => _ShowLVMotherSupplyPointPanel;
            set => SetProperty(ref _ShowLVMotherSupplyPointPanel, value);
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
                        try
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
                                else if (this.UpdateMode == ADMSUpdateMode.Pole)
                                {
                                    using (var cursor = fLayer.Search(new QueryFilter() { ObjectIDs = mapMemberSelection.Value }))
                                    {
                                        while (cursor.MoveNext())
                                        {
                                            var element = un.CreateElement(cursor.Current);
                                            if (element.AssetGroup.Name == "HV Switch" && (element.AssetType.Name == "Isolator" || element.AssetType.Name == "Switch")
                                            || element.AssetGroup.Name == "Transformer" && element.AssetType.Name == "HV PM TX"
                                            || element.AssetGroup.Name == "HV Fuse" && element.AssetType.Name == "Fuse"
                                            || element.AssetGroup.Name == "HV Switch" && element.AssetType.Name == "Subring Circuit Breaker")
                                            {
                                                selectionElements.Add(element);
                                            }
                                        }
                                    }
                                }
                                else if (this.UpdateMode == ADMSUpdateMode.PoleCable)
                                {
                                    using (var cursor = fLayer.Search(new QueryFilter() { ObjectIDs = mapMemberSelection.Value }))
                                    {
                                        while (cursor.MoveNext())
                                        {
                                            var element = un.CreateElement(cursor.Current);
                                            if (element.AssetGroup.Name == "HV Line" && (element.AssetType.Name == "Cable" || element.AssetType.Name == "Overhead Line"))
                                            {
                                                selectionElements.Add(element);
                                            }
                                        }
                                    }
                                }
                                else if (this.UpdateMode == ADMSUpdateMode.LVFeature)
                                {
                                    using (var cursor = fLayer.Search(new QueryFilter() { ObjectIDs = mapMemberSelection.Value }))
                                    {
                                        while (cursor.MoveNext())
                                        {
                                            var element = un.CreateElement(cursor.Current);
                                            if ((element.AssetGroup.Name == "LV Fuse" && (element.AssetType.Name == "Fuse" || element.AssetType.Name == "Source Fuse")) ||
                                                (element.AssetGroup.Name == "LV Service Point" && (element.AssetType.Name == "Supply Point" || element.AssetType.Name == "Local Supply")) ||
                                                (element.AssetGroup.Name == "LV Switch" && element.AssetType.Name == "Switch"))
                                            {
                                                selectionElements.Add(element);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            LoggerHelper.Error(e, $"Fail in function OnMapSelectionChanged: {e.Message}");
                            MessageBox.Show(e.Message);
                            break;
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
                startElements.First().Terminal = startElements.First().AssetType.GetTerminalConfiguration().Terminals.FirstOrDefault(p => p.Name == "CB:Bus Side" || p.Name== "Source" || p.Name == "SS:S1");
                startElements.Last().Terminal = startElements.Last().AssetType.GetTerminalConfiguration().Terminals.FirstOrDefault(p => p.Name == "CB:Line Side" || p.Name == "Load" || p.Name == "SS:S2");
            }
            Tier sourceTier = domainNetwork.GetTier("HV");
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
                    if (!hvSwitchModel.SSNAME.Contains("CUST EQPT"))
                    {
                        var busBars = features.Where(p => p.IsHVBusbar);
                        var busNodes = features.Where(p => p.IsHVBusNode);
                        if (busBars.Any())
                        {
                            hvSwitchModel.Busbar = busBars.FirstOrDefault();
                            String traceInfo = $"Trace BusBars INFO\nSWitch:[{startElements.First().ObjectID},{startElements.First().GlobalID}],BusBars :[{String.Join(",", busBars.Select(p => $"{p.ObjectID},{p.GlobalID}"))}]";
                            LoggerHelper.Info(traceInfo);
                        }
                        if (busNodes.Any())
                        {
                            hvSwitchModel.BusNodes = busNodes.ToList();
                        }
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
                if (SelectionElement == null || (this.UpdateMode != ADMSUpdateMode.PoleCable && this.SelectionElements.Count == 0)) return;
                var utilityNetwork = MapView.Active?.Map
                    .GetLayersAsFlattenedList()
                    .OfType<UtilityNetworkLayer>().FirstOrDefault()?.GetUtilityNetwork();
                LoggerHelper.Info($"Selected model: {this.UpdateMode}");
                using (UtilityNetworkDefinition utilityNetworkDefinition = utilityNetwork.GetDefinition())
                {
                    using (NetworkSource networkSource = utilityNetworkDefinition.GetNetworkSource("ElectricDevice"))
                    {
                        try
                        {
                            if (this.UpdateMode == ADMSUpdateMode.SS_TO_SS)
                            {
                                DomainNetwork domainNetwork = utilityNetworkDefinition.GetDomainNetwork("Electric");
                                Tier sourceTier = domainNetwork.GetTier("HV");
                                LoggerHelper.Info($"Trace start at: {DateTime.Now}");
                                TraceConfiguration cfg = sourceTier.GetTraceConfiguration();
                                cfg.Propagators = new List<Propagator>();
                                var catSub = utilityNetworkDefinition
                                    .GetAvailableCategories()
                                    .FirstOrDefault(c => c.Equals("E:Switch", StringComparison.OrdinalIgnoreCase));
                                cfg.Filter.Scope = TraversabilityScope.JunctionsAndEdges;
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
                                    LoggerHelper.Info($"Trace end at: {DateTime.Now}");
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
                                    LoggerHelper.Info($"Get Association Info start at: {DateTime.Now}");
                                    foreach (var hvSwitchAssociation in hvSwitchAssociations.Associations)
                                    {
                                        if (hvSwitchAssociation.FromElement.AssetGroup.Name == "Substation"
                                        && hvSwitchAssociation.ToElement.AssetGroup.Name == "HV Switch")
                                        {
                                            if (first == null)
                                            {
                                                var firstSwitchFeature = features.FirstOrDefault(p => 
                                                    p.Element.GlobalID == hvSwitchAssociation.ToElement.GlobalID);
                                                var firstSubstation = features.FirstOrDefault(p => 
                                                    p.Element.GlobalID == hvSwitchAssociation.FromElement.GlobalID);
                                                first = new SS_TO_SS_Model(firstSwitchFeature, utilityNetwork);
                                                first.SSCODE = firstSubstation.Attributes["SSNUM"]?.ToString();
                                                first.SSNAME = firstSubstation.Attributes["SSNAME"]?.ToString();
                                                first.Source = firstSwitchFeature;
                                                first.Substation = firstSubstation;
                                            }
                                            else
                                            {
                                                var secondSwitchFeature = features.FirstOrDefault(p => 
                                                    p.Element.GlobalID == hvSwitchAssociation.ToElement.GlobalID);
                                                var secondSubstation = features.FirstOrDefault(p => 
                                                    p.Element.GlobalID == hvSwitchAssociation.FromElement.GlobalID);
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
                                                    var transformerAssociations = utilityNetwork.GetAssociations(transformerElement, 
                                                        AssociationType.Containment);
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
                                        if (hvSwitchAssociation.FromElement.AssetGroup.Name == "HV Switching Assembly"
                                        && hvSwitchAssociation.ToElement.AssetGroup.Name == "HV Switch")
                                        {
                                            var assemblyAssociations = utilityNetwork.GetAssociations(hvSwitchAssociation.FromElement, AssociationType.Containment);
                                            string msg = $"HV Switching Assembly Assictions:[{String.Join(",", assemblyAssociations.Select(p => p.FromElement.AssetGroup.Name))}]";
                                            LoggerHelper.Info(msg);
                                            foreach( var assemblyAssociation in assemblyAssociations)
                                            {
                                                if(assemblyAssociation.FromElement.AssetGroup.Name == "Substation")
                                                {
                                                    var firstSwitchFeature = features.FirstOrDefault(p =>
                                                            p.Element.GlobalID == hvSwitchAssociation.ToElement.GlobalID);
                                                    var firstSubstation = features.FirstOrDefault(p =>
                                                            p.Element.GlobalID == assemblyAssociation.FromElement.GlobalID);
                                                    var firstSource = features.FirstOrDefault(p => 
                                                            p.Element.GlobalID == hvSwitchAssociation.FromElement.GlobalID);
                                                    if (first == null)
                                                    {
                                                        first = new SS_TO_SS_Model(firstSwitchFeature, utilityNetwork);
                                                        first.SSCODE = firstSubstation.Attributes["SSNUM"]?.ToString();
                                                        first.SSNAME = firstSubstation.Attributes["SSNAME"]?.ToString();
                                                        first.Source = firstSwitchFeature;
                                                        first.Substation = firstSource;
                                                    } else
                                                    {
                                                        second = new SS_TO_SS_Model(firstSwitchFeature, utilityNetwork);
                                                        second.SSCODE = firstSubstation.Attributes["SSNUM"]?.ToString();
                                                        second.SSNAME = firstSubstation.Attributes["SSNAME"]?.ToString();
                                                        second.Source = firstSwitchFeature;
                                                        second.Substation = firstSource;
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    LoggerHelper.Info($"Get Association Info end at: {DateTime.Now}");
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

                                        if(first.SSNAME.CompareTo(second.SSNAME) > 0)
                                        {
                                            var tmp = first;
                                            first = second;
                                            second = tmp;
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

                                        this.Cables = features.Where(p => p.AssetGroupName == "HV Line" && p.AssetTypeName == "Cable" 
                                        || (p.AssetGroupName == "HV Connection Point" && (p.AssetTypeName == "Termination" || p.AssetTypeName == "Joint")));
                                        foreach (var cable in Cables)
                                        {
                                            if(cable.AssetTypeName == "Cable")
                                            {
                                                cable.Attributes["ADMS_Name"] = ADMSUpdateHelper.GetCableADMSName(first, second, cable);
                                                cable.Attributes["ADMS_Alias"] = ADMSUpdateHelper.GetCableADMSAlias(first, second, cable);
                                            }
                                            cable.Attributes["terminated_substation"] = ADMSUpdateHelper.GetCable_Terminal_Substation(first, second);
                                            
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
                                        this.Cables = features.Where(p => p.AssetGroupName == "HV Line" && p.AssetTypeName == "Cable" 
                                        || (p.AssetGroupName == "HV Connection Point" && (p.AssetTypeName == "Termination" || p.AssetTypeName == "Joint")));
                                        foreach (var cable in Cables)
                                        {
                                            if(cable.AssetTypeName == "Cable")
                                            {
                                                cable.Attributes["ADMS_Name"] = ADMSUpdateHelper.GetCableADMSName(first, second, cable);
                                                cable.Attributes["ADMS_Alias"] = ADMSUpdateHelper.GetCableADMSAlias(first, second, cable);
                                            }
                                            cable.Attributes["terminated_substation"] = ADMSUpdateHelper.GetCable_Terminal_Substation(first, second);
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
                                LoggerHelper.Info($"Starting to process Spare HV Switch at {DateTime.Now}");
                                var startElement = this.SelectionElement;
                                LoggerHelper.Info($"Starting to get Spare HV Switch Association at {DateTime.Now}");
                                var hvSwitchAssociations = utilityNetwork.GetAssociations(startElement);
                                LoggerHelper.Info($"Ending to get Spare HV Switch Association at {DateTime.Now}");
                                SS_TO_SS_Model first = null;
                                if (hvSwitchAssociations.Count() == 0)
                                {
                                    MessageBox.Show("No accociation in CB.");
                                    return;
                                }
                                foreach (var hvSwitchAssociation in hvSwitchAssociations)
                                {
                                    if (hvSwitchAssociation.FromElement.AssetGroup.Name == "Substation" && 
                                        hvSwitchAssociation.ToElement.AssetGroup.Name == "HV Switch")
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
                                        // Now you can run selections, queries, etc.
                                        FeatureSnapshot firstSwitchFeature = null;
                                        FeatureSnapshot firstSubstationFeature = null;
                                        LoggerHelper.Info($"Starting to query target element (switch) info at {DateTime.Now}");
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
                                        LoggerHelper.Info($"Ending to query target element (switch) info at {DateTime.Now}");
                                        LoggerHelper.Info($"Starting to query target element (substation) info at {DateTime.Now}");
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
                                        LoggerHelper.Info($"Ending to query target element (substation) info at {DateTime.Now}");
                                        first = new SS_TO_SS_Model(firstSwitchFeature, utilityNetwork);
                                        first.SSCODE = firstSubstationFeature.Attributes["SSNUM"]?.ToString();
                                        first.SSNAME = firstSubstationFeature.Attributes["SSNAME"]?.ToString();
                                        first.Source = firstSwitchFeature;
                                        first.Substation = firstSubstationFeature;
                                        this.SpareHVSwitch = first;
                                        this.ShowSearchPanel = false;
                                        this.ShowSpareCBUpdatePanel = true;
                                        break;
                                    }
                                }
                                LoggerHelper.Info($"Ending to process Spare HV Switch at {DateTime.Now}");
                                if (first == null)
                                {
                                    MessageBox.Show("No accociation between CB and Substation.");
                                    return;
                                }
                                    
                            }
                            else if (this.UpdateMode == ADMSUpdateMode.Pole)
                            {
                                var startElement = this.SelectionElement;
                                LoggerHelper.Info($"Starting to get Pole/Substation feature Association at {DateTime.Now}");
                                var elementAssociations = utilityNetwork.GetAssociations(startElement);
                                Pole_Model first = null;
                                bool isSingleDevice = false;
                                foreach (var elementAssociation in elementAssociations)
                                {
                                    if(elementAssociation.FromElement.AssetGroup.Name == "Support Structure")
                                    {
                                        var deviceLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(l => l.Name == elementAssociation.ToElement.AssetGroup.Name);
                                        var substationLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(l => l.Name == elementAssociation.FromElement.AssetGroup.Name);
                                        if (deviceLayer == null)
                                        {
                                            MessageBox.Show($"Fail to found layer {elementAssociation.ToElement.AssetGroup.Name}.");
                                            return;
                                        }
                                        if (substationLayer == null)
                                        {
                                            MessageBox.Show($"Fail to found layer {elementAssociation.FromElement.AssetGroup.Name}.");
                                            return;
                                        }
                                        //deviceLayer.GetFeatureClass().Search();
                                        // Now you can run selections, queries, etc.
                                        FeatureSnapshot firstSwitchFeature = null;
                                        FeatureSnapshot firstPoleFeature = null;
                                        Row txAttributes = null;
                                        var qf = new QueryFilter { WhereClause = "GLOBALID = '{" + elementAssociation.ToElement.GlobalID + "}'" };
                                        using (var switchCursor = deviceLayer.GetFeatureClass().Search(qf))
                                        {
                                            if (switchCursor.MoveNext())
                                            {
                                                var row = switchCursor.Current;
                                                var element = utilityNetwork.CreateElement(row);
                                                var results = new SpatialSubgraphExtractor(utilityNetwork).Extract([element]);
                                                var features = results.FeatureByGlobalId.Values;
                                                firstSwitchFeature = features.FirstOrDefault(p => p.Element.GlobalID == elementAssociation.ToElement.GlobalID);
                                            }
                                        }

                                        qf.WhereClause = "GLOBALID = '{" + elementAssociation.FromElement.GlobalID + "}'";
                                        using (var substationCusor = substationLayer.GetFeatureClass().Search(qf))
                                        {

                                            if (substationCusor.MoveNext())
                                            {
                                                var row = substationCusor.Current;
                                                var element = utilityNetwork.CreateElement(row);
                                                var results = new SpatialSubgraphExtractor(utilityNetwork).Extract([element]);
                                                var features = results.FeatureByGlobalId.Values;
                                                firstPoleFeature = features.FirstOrDefault(p => p.Element.GlobalID == elementAssociation.FromElement.GlobalID);
                                            }
                                        }

                                        if(firstSwitchFeature.AssetGroupName != "Transformer")
                                        {
                                            var poleAssociations = utilityNetwork.GetAssociations(firstPoleFeature.Element);
                                            if (poleAssociations.Where(i => i.ToElement.AssetGroup.Name == "Transformer" ||
                                                                    i.ToElement.AssetGroup.Name == "HV Switch" ||
                                                                    i.ToElement.AssetGroup.Name == "HV Fuse").ToList().Count == 1)
                                                isSingleDevice = true;
                                            foreach (var poleAssociation in poleAssociations)
                                            {
                                                if (poleAssociation.ToElement.AssetGroup.Name == "Transformer" ||
                                                    (poleAssociation.ToElement.AssetGroup.Name == "HV Switch" &&
                                                    poleAssociation.ToElement.AssetType.Name == "Switch"))
                                                {
                                                    var layer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(l => l.Name == elementAssociation.ToElement.AssetGroup.Name);
                                                    if (layer == null)
                                                    {
                                                        MessageBox.Show($"Fail to found layer {poleAssociation.ToElement.AssetGroup.Name}.");
                                                        return;
                                                    }
                                                    var qf_tx = new QueryFilter { WhereClause = "GLOBALID = '{" + poleAssociation.ToElement.GlobalID + "}'" };
                                                    using (var switchCursor = layer.GetFeatureClass().Search(qf_tx))
                                                    {
                                                        if (switchCursor.MoveNext())
                                                        {
                                                            var row = switchCursor.Current;
                                                            txAttributes = row;
                                                        }
                                                    }
                                                    break;
                                                }
                                            }
                                        }


                                        first = new Pole_Model(firstSwitchFeature, utilityNetwork);
                                        first.CIRCUIT_NAME = firstPoleFeature.Attributes["circuitname"]?.ToString();
                                        first.FROM_POLE_NUM = firstPoleFeature.Attributes["polenum"]?.ToString();
                                        first.Source = firstSwitchFeature;
                                        first.Pole = firstPoleFeature;
                                        first.IsSingleDevice = isSingleDevice;
                                        if (txAttributes != null)
                                        {
                                            first.FROM_SS_NUM = txAttributes["SSNUM"] != null ? $"{txAttributes["SSNUM"]}" : "";
                                            first.FROM_SS_NAME = txAttributes["SSNAME"] != null ? $"{txAttributes["SSNAME"]}" : "";
                                            first.IsTxInPole = true;
                                        }
                                        this.PoleDevice = first;
                                        this.ShowSearchPanel = false;
                                        this.ShowPolePanel = true;
                                        break;
                                    }
                                    else if (elementAssociation.FromElement.AssetGroup.Name == "Substation" 
                                    && elementAssociation.ToElement.AssetType.Name == "Subring Circuit Breaker")
                                    {
                                        var deviceLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(l => l.Name == elementAssociation.ToElement.AssetGroup.Name);
                                        var substationLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(l => l.Name == elementAssociation.FromElement.AssetGroup.Name);
                                        if (deviceLayer == null)
                                        {
                                            MessageBox.Show($"Fail to found layer {elementAssociation.ToElement.AssetGroup.Name}.");
                                            return;
                                        }
                                        if (substationLayer == null)
                                        {
                                            MessageBox.Show($"Fail to found layer {elementAssociation.FromElement.AssetGroup.Name}.");
                                            return;
                                        }

                                        FeatureSnapshot firstSwitchFeature = null;
                                        FeatureSnapshot firstSubstationFeature = null;

                                        var qf = new QueryFilter { WhereClause = "GLOBALID = '{" + elementAssociation.ToElement.GlobalID + "}'" };
                                        using (var switchCursor = deviceLayer.GetFeatureClass().Search(qf))
                                        {
                                            if (switchCursor.MoveNext())
                                            {
                                                var row = switchCursor.Current;
                                                var element = utilityNetwork.CreateElement(row);
                                                var results = new SpatialSubgraphExtractor(utilityNetwork).Extract([element]);
                                                var features = results.FeatureByGlobalId.Values;
                                                firstSwitchFeature = features.FirstOrDefault(p => p.Element.GlobalID == elementAssociation.ToElement.GlobalID);
                                            }
                                        }

                                        qf.WhereClause = "GLOBALID = '{" + elementAssociation.FromElement.GlobalID + "}'";
                                        using (var substationCusor = substationLayer.GetFeatureClass().Search(qf))
                                        {

                                            if (substationCusor.MoveNext())
                                            {
                                                var row = substationCusor.Current;
                                                var element = utilityNetwork.CreateElement(row);
                                                var results = new SpatialSubgraphExtractor(utilityNetwork).Extract([element]);
                                                var features = results.FeatureByGlobalId.Values;
                                                firstSubstationFeature = features.FirstOrDefault(p => p.Element.GlobalID == elementAssociation.FromElement.GlobalID);
                                            }
                                        }

                                        first = new Pole_Model(firstSwitchFeature, utilityNetwork);
                                        first.FROM_SS_NAME = firstSubstationFeature.Attributes["SSNAME"]?.ToString();
                                        first.FROM_SS_NUM = firstSubstationFeature.Attributes["SSNUM"]?.ToString();
                                        first.FROM_POLE_NUM = firstSwitchFeature.Attributes["PANEL_NO"]?.ToString();
                                        first.Source = firstSwitchFeature;
                                        first.Pole = firstSubstationFeature;

                                        this.PoleDevice = first;
                                        this.ShowSearchPanel = false;
                                        this.ShowPolePanel = true;
                                        break;
                                    }
                                }
                                LoggerHelper.Info($"Ending to get Pole/Substation feature Association at {DateTime.Now}");
                            }
                            else if (this.UpdateMode == ADMSUpdateMode.PoleCable)
                            {
                                this.ShowSearchPanel = false;
                                this.ShowPoleCablePanel = true;
                            }
                            else if (this.UpdateMode == ADMSUpdateMode.LVFeature)
                            {
                                var startElement = this.SelectionElement;
                                this.LVFeature = null;
                                this.LVFeatureContainer = null;
                                this.ShowLVSourceFusePanel = false;
                                this.ShowLVSupplyPointPanel = false;
                                this.ShowLVPillarFusePanel = false;
                                this.ShowLVLinkBoxPanel = false;
                                this.ShowLVMotherSupplyPointPanel = false;
                                string assetGroup = startElement.AssetGroup.Name;
                                string assetType = startElement.AssetType.Name;
                                if (!((assetGroup == "LV Fuse" && assetType == "Source Fuse") ||
                                    (assetGroup == "LV Service Point" && assetType == "Supply Point") ||
                                    (assetGroup == "LV Fuse" && assetType == "Fuse") ||
                                    (assetGroup == "LV Switch" && assetType == "Switch")))
                                {
                                    MessageBox.Show("Only LV Source Fuse, Supply Point, Pillar Fuse and Link Box Switch are supported now.", "Invalid Selection", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                                    return;
                                }
                                if (assetGroup == "LV Fuse" && assetType == "Source Fuse")
                                {
                                    DomainNetwork domainNetwork = utilityNetworkDefinition.GetDomainNetwork("Electric");
                                    Tier sourceTier = domainNetwork.GetTier("LV");
                                    LoggerHelper.Info($"Trace start at: {DateTime.Now}");
                                    TraceConfiguration cfg = sourceTier.GetTraceConfiguration();
                                    cfg.Propagators = new List<Propagator>();
                                    var catSub = utilityNetworkDefinition
                                        .GetAvailableCategories()
                                        .FirstOrDefault(c => c.Equals("Subnetwork Controller", StringComparison.OrdinalIgnoreCase));
                                    cfg.Filter.Scope = TraversabilityScope.JunctionsAndEdges;
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
                                        if (assetGroup == "LV Fuse" && assetType == "Source Fuse")
                                        {
                                            var tcfg = startElement.AssetType.GetTerminalConfiguration();
                                            startElement.Terminal = tcfg.Terminals.FirstOrDefault(p => p.Name == "Source");
                                        }

                                        TraceArgument traceArgument = new TraceArgument(new List<Element>() { this.SelectionElement });
                                        traceArgument.Configuration = cfg;
                                        Tracer tracer = traceManager.GetTracer<ConnectedTracer>();
                                        IReadOnlyList<Result> traceResults = tracer.Trace(traceArgument);
                                        var results = new SpatialSubgraphExtractor(utilityNetwork).ExtractFromResults(traceResults);
                                        LoggerHelper.Info($"Trace end at: {DateTime.Now}");
                                        await HighlightPathOnMapAsync(utilityNetwork, results.FeatureByGlobalId.Values);

                                        var features = results.FeatureByGlobalId.Values;
                                        var transfomers = features.Where(p => p.AssetGroupName == "Transformer");

                                        if (transfomers.Count() == 0)
                                        {
                                            MessageBox.Show("The process cannot be completed because there are no Transformer ", "Invalid Selection", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                                            return;
                                        }

                                        var sourceFuseFeatures = features
                                            .Where(p => p.AssetGroupName == "LV Fuse" && p.AssetTypeName == "Source Fuse")
                                            .ToList();
                                        var selectedSourceFuseFeature = sourceFuseFeatures.FirstOrDefault(p => p.Element.GlobalID == startElement.GlobalID);
                                        if (selectedSourceFuseFeature == null)
                                        {
                                            MessageBox.Show("The process cannot be completed because the selected Source Fuse is not in the trace result.", "Invalid Selection", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                                            return;
                                        }

                                        var localSupplyFeatures = features
                                            .Where(p => p.AssetGroupName == "LV Service Point" && (p.AssetTypeName == "Local Supply" || p.AssetTypeName == "Local Supply Point"))
                                            .ToList();
                                        var transformer = transfomers.FirstOrDefault();
                                        string txNo = transformer?.GetString("TX_NO");
                                        string ssName = transformer?.GetString("SSNAME");
                                        string ssNum = transformer?.GetString("SSNUM");
                                        string transformerSSName = ssName;
                                        string transformerSSNum = ssNum;

                                        var transformerAssociations = utilityNetwork.TraverseAssociations(transfomers.Select(p => p.Element), new TraverseAssociationsDescription(TraversalDirection.Ascending));
                                        bool isPoleSourceFuse = transformerAssociations.Associations.Any(p =>
                                            p.FromElement.AssetGroup.Name == "Support Structure"
                                            && (p.FromElement.AssetType.Name == "HV Pole" || p.FromElement.AssetType.Name == "LV Pole"));
                                        LoggerHelper.Info($"Get Association Info start at: {DateTime.Now}");
                                        foreach (var transformerAssociation in transformerAssociations.Associations)
                                        {
                                            if (!isPoleSourceFuse && transformerAssociation.FromElement.AssetGroup.Name == "Substation"
                                            && transformerAssociation.ToElement.AssetGroup.Name == "Transformer")
                                            {
                                                using (var substationTable = utilityNetwork.GetTable(transformerAssociation.FromElement.NetworkSource))
                                                {
                                                    var substationFields = substationTable.GetDefinition().GetFields().Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                                                    using (var cursor = substationTable.Search(new QueryFilter() { ObjectIDs = new List<long>() { transformerAssociation.FromElement.ObjectID } }, false))
                                                    {
                                                        if (cursor.MoveNext())
                                                        {
                                                            using (var row = cursor.Current)
                                                            {
                                                                if (substationFields.Contains("SSNUM"))
                                                                {
                                                                    ssNum = row["SSNUM"]?.ToString();
                                                                }
                                                                if (substationFields.Contains("SSNAME"))
                                                                {
                                                                    ssName = row["SSNAME"]?.ToString();
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                break;
                                            }
                                            else if (transformerAssociation.FromElement.AssetGroup.Name == "Support Structure"
                                            && (transformerAssociation.FromElement.AssetType.Name == "HV Pole" || transformerAssociation.FromElement.AssetType.Name == "LV Pole")
                                            && transformerAssociation.ToElement.AssetGroup.Name == "Transformer")
                                            {
                                                isPoleSourceFuse = true;
                                                ssName = transformerSSName;
                                                ssNum = transformerSSNum;
                                            }
                                        }
                                        LoggerHelper.Info($"Get Association Info end at: {DateTime.Now}");
                                        LVFeature_Model CreateLVFeatureModel(FeatureSnapshot feature)
                                        {
                                            var model = new LVFeature_Model(feature, utilityNetwork);
                                            model.TX_NO = txNo;
                                            model.SS_NAME = ssName;
                                            model.SS_NUM = ssNum;
                                            model.IsPoleSourceFuse = isPoleSourceFuse;
                                            return model;
                                        }

                                        var sourceFuseModels = sourceFuseFeatures.Select(CreateLVFeatureModel).ToList();
                                        var selectedSourceFuse = sourceFuseModels.FirstOrDefault(p => p.Source.Element.GlobalID == startElement.GlobalID);
                                        var localSupplyModels = localSupplyFeatures.Select(CreateLVFeatureModel).ToList();

                                        this.LVFeature = selectedSourceFuse;
                                        this.LVFeatureContainer = new LVFeatureContainer_Model(sourceFuseModels, localSupplyModels, selectedSourceFuse);
                                        this.ShowLVSourceFusePanel = true;
                                    }

                                }
                                else if (assetGroup == "LV Service Point" && assetType == "Supply Point")
                                {
                                    var results = new SpatialSubgraphExtractor(utilityNetwork).Extract(new List<Element>() { startElement });
                                    var supplyPointFeature = results.FeatureByGlobalId.Values.FirstOrDefault(p => p.Element.GlobalID == startElement.GlobalID);
                                    if (supplyPointFeature == null)
                                    {
                                        MessageBox.Show("The process cannot be completed because the selected Supply Point cannot be loaded.", "Invalid Selection", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                                        return;
                                    }

                                    this.LVFeature = new LVFeature_Model(supplyPointFeature, utilityNetwork);
                                    this.ShowLVSupplyPointPanel = true;
                                }
                                else if (assetGroup == "LV Fuse" && assetType == "Fuse")
                                {
                                    var ascendingAssociations = utilityNetwork.TraverseAssociations(new List<Element>() { startElement }, new TraverseAssociationsDescription(TraversalDirection.Ascending));
                                    var descendingAssociations = utilityNetwork.TraverseAssociations(new List<Element>() { startElement }, new TraverseAssociationsDescription(TraversalDirection.Descending));
                                    var pillarCircuitBoxElement = ascendingAssociations.Associations
                                        .Concat(descendingAssociations.Associations)
                                        .SelectMany(p => new List<Element>() { p.FromElement, p.ToElement })
                                        .FirstOrDefault(p => p.AssetType.Name == "Pillar Circuit Box");
                                    var poleElement = ascendingAssociations.Associations
                                        .Concat(descendingAssociations.Associations)
                                        .SelectMany(p => new List<Element>() { p.FromElement, p.ToElement })
                                        .FirstOrDefault(p => p.AssetType.Name == "HV Pole" || p.AssetType.Name == "LV Pole");
                                    if (pillarCircuitBoxElement != null)
                                    {
                                        var pillarResults = new SpatialSubgraphExtractor(utilityNetwork).Extract(new List<Element>() { pillarCircuitBoxElement });
                                        var pillarCircuitBoxFeature = pillarResults.FeatureByGlobalId.Values.FirstOrDefault(p => p.Element.GlobalID == pillarCircuitBoxElement.GlobalID);
                                        if (pillarCircuitBoxFeature == null)
                                        {
                                            return;
                                        }

                                        DomainNetwork domainNetwork = utilityNetworkDefinition.GetDomainNetwork("Electric");
                                        Tier sourceTier = domainNetwork.GetTier("LV");
                                        LoggerHelper.Info($"Trace start at: {DateTime.Now}");
                                        TraceConfiguration cfg = sourceTier.GetTraceConfiguration();
                                        cfg.Propagators = new List<Propagator>();
                                        var catSwitchFuse = utilityNetworkDefinition
                                            .GetAvailableCategories()
                                            .FirstOrDefault(c => c.Equals("E:Switch - Fuse", StringComparison.OrdinalIgnoreCase));
                                        cfg.Filter.Scope = TraversabilityScope.JunctionsAndEdges;
                                        if (catSwitchFuse != null)
                                        {
                                            var catExpr = new CategoryComparison(CategoryOperator.IsEqual, catSwitchFuse);
                                            cfg.Traversability.Barriers = (Condition)catExpr;
                                        }

                                        var tcfg = startElement.AssetType.GetTerminalConfiguration();
                                        startElement.Terminal = tcfg.Terminals.FirstOrDefault(p => p.Name == "Node 1");

                                        using (TraceManager traceManager = utilityNetwork.GetTraceManager())
                                        {
                                            TraceArgument traceArgument = new TraceArgument(new List<Element>() { startElement });
                                            traceArgument.Configuration = cfg;
                                            Tracer tracer = traceManager.GetTracer<ConnectedTracer>();
                                            IReadOnlyList<Result> traceResults = tracer.Trace(traceArgument);
                                            var results = new SpatialSubgraphExtractor(utilityNetwork).ExtractFromResults(traceResults);
                                            LoggerHelper.Info($"Trace end at: {DateTime.Now}");
                                            await HighlightPathOnMapAsync(utilityNetwork, results.FeatureByGlobalId.Values);

                                            var features = results.FeatureByGlobalId.Values;
                                            var pillarFuseFeatures = features
                                                .Where(p => p.AssetGroupName == "LV Fuse" && p.AssetTypeName == "Fuse")
                                                .ToList();

                                            var pillarCircuitBox = new LVFeature_Model(pillarCircuitBoxFeature, utilityNetwork);
                                            LVFeature_Model CreatePillarFuseModel(FeatureSnapshot feature)
                                            {
                                                var model = new LVFeature_Model(feature, utilityNetwork);
                                                model.PR_NO = pillarCircuitBox.PR_NO;
                                                model.PR_NAME = pillarCircuitBox.PR_NAME;
                                                return model;
                                            }

                                            var pillarFuseModels = pillarFuseFeatures.Select(CreatePillarFuseModel).ToList();
                                            var selectedPillarFuse = pillarFuseModels.FirstOrDefault(p => p.Source.Element.GlobalID == startElement.GlobalID);
                                            this.LVFeature = selectedPillarFuse;
                                            this.LVFeatureContainer = new LVFeatureContainer_Model(null, null, null, pillarFuseModels, selectedPillarFuse, pillarCircuitBox);
                                            this.ShowLVPillarFusePanel = true;
                                        }
                                    }
                                    else if (poleElement != null) 
                                    {
                                        var poleResults = new SpatialSubgraphExtractor(utilityNetwork).Extract(new List<Element>() { poleElement });
                                        var poleFeature = poleResults.FeatureByGlobalId.Values.FirstOrDefault(p => p.Element.GlobalID == poleElement.GlobalID);
                                        if (poleFeature == null)
                                        {
                                            return;
                                        }

                                        var selectedFuseResults = new SpatialSubgraphExtractor(utilityNetwork).Extract(new List<Element>() { startElement });
                                        var selectedFuseFeature = selectedFuseResults.FeatureByGlobalId.Values.FirstOrDefault(p => p.Element.GlobalID == startElement.GlobalID);
                                        if (selectedFuseFeature == null)
                                        {
                                            return;
                                        }

                                        DomainNetwork domainNetwork = utilityNetworkDefinition.GetDomainNetwork("Electric");
                                        Tier sourceTier = domainNetwork.GetTier("LV");
                                        var tcfg = startElement.AssetType.GetTerminalConfiguration();
                                        startElement.Terminal = tcfg.Terminals.FirstOrDefault(p => p.Name == "Node 1");
                                        TraceConfiguration cfg = sourceTier.GetTraceConfiguration();
                                        cfg.Propagators = new List<Propagator>();
                                        cfg.Filter.Scope = TraversabilityScope.JunctionsAndEdges;

                                        var catSub = utilityNetworkDefinition
                                            .GetAvailableCategories()
                                            .FirstOrDefault(c => c.Equals("Subnetwork Controller", StringComparison.OrdinalIgnoreCase));
                                        if (catSub != null)
                                        {
                                            cfg.Traversability.Barriers = (Condition)new CategoryComparison(CategoryOperator.IsEqual, catSub);
                                        }

                                        var normalOperatingStatusAttr = TraceCfgHelpers.FindNetworkAttribute(utilityNetworkDefinition, "NormalOperatingStatus", "Normal Operating Status");
                                        if (normalOperatingStatusAttr != null)
                                        {
                                            var openExpr = new NetworkAttributeComparison(normalOperatingStatusAttr, Operator.Equal, (int)NormalOperatingStatus.Open);
                                            var existing = cfg.Traversability.Barriers as ConditionalExpression;
                                            cfg.Traversability.Barriers = existing == null ? (Condition)openExpr : new Or(existing, openExpr);
                                        }

                                        var lifeCycleStatusAttr = TraceCfgHelpers.FindNetworkAttribute(utilityNetworkDefinition, "LifeCycleStatus", "Life Cycle Status");
                                        if (lifeCycleStatusAttr != null)
                                        {
                                            var notInServiceStatuses = new List<int> { 0, 1, 3, 4 };
                                            foreach (var status in notInServiceStatuses)
                                            {
                                                var statusExpr = new NetworkAttributeComparison(lifeCycleStatusAttr, Operator.Equal, status);
                                                var existing = cfg.Traversability.Barriers as ConditionalExpression;
                                                cfg.Traversability.Barriers = existing == null ? (Condition)statusExpr : new Or(existing, statusExpr);
                                            }
                                        }

                                        using (TraceManager traceManager = utilityNetwork.GetTraceManager())
                                        {
                                            TraceArgument traceArgument = new TraceArgument(new List<Element>() { startElement });
                                            traceArgument.Configuration = cfg;
                                            Tracer tracer = traceManager.GetTracer<ConnectedTracer>();
                                            IReadOnlyList<Result> traceResults = tracer.Trace(traceArgument);
                                            var results = new SpatialSubgraphExtractor(utilityNetwork).ExtractFromResults(traceResults);
                                            LoggerHelper.Info($"Trace end at: {DateTime.Now}");

                                            var sourceFuseFeature = results.FeatureByGlobalId.Values.FirstOrDefault(p =>
                                                p.AssetGroupName == "LV Fuse"
                                                && p.AssetTypeName == "Source Fuse"
                                                && p.NormalOperatingStatus == NormalOperatingStatus.Closed);
                                            if (sourceFuseFeature == null)
                                            {
                                                return;
                                            }

                                            var sourceFuseElement = sourceFuseFeature.Element;
                                            tcfg = sourceFuseElement.AssetType.GetTerminalConfiguration();
                                            sourceFuseElement.Terminal = tcfg.Terminals.FirstOrDefault(p => p.Name == "Source");

                                            TraceConfiguration sourceFuseCfg = sourceTier.GetTraceConfiguration();
                                            sourceFuseCfg.Propagators = new List<Propagator>();
                                            sourceFuseCfg.Filter.Scope = TraversabilityScope.JunctionsAndEdges;
                                            var sourceFuseCatSub = utilityNetworkDefinition
                                                .GetAvailableCategories()
                                                .FirstOrDefault(c => c.Equals("Subnetwork Controller", StringComparison.OrdinalIgnoreCase));
                                            if (sourceFuseCatSub != null)
                                            {
                                                var catExpr = new CategoryComparison(CategoryOperator.IsEqual, sourceFuseCatSub);
                                                var existing = sourceFuseCfg.Traversability.Barriers as ConditionalExpression;
                                                sourceFuseCfg.Traversability.Barriers = existing == null ? (Condition)catExpr : new Or(existing, catExpr);
                                            }

                                            sourceFuseCfg.Traversability.Barriers = TraceCfgHelpers.RemoveAttrFromBarriers(sourceFuseCfg.Traversability.Barriers, new string[] { "NormalOperatingStatus", "Life Cycle Status" });
                                            var lifeCycleStatuses = new List<int> { 0, 4, 3 };
                                            foreach (var status in lifeCycleStatuses)
                                            {
                                                if (lifeCycleStatusAttr != null)
                                                {
                                                    var statusExpr = new NetworkAttributeComparison(lifeCycleStatusAttr, Operator.Equal, status);
                                                    var existing = sourceFuseCfg.Traversability.Barriers as ConditionalExpression;
                                                    sourceFuseCfg.Traversability.Barriers = existing == null ? (Condition)statusExpr : new Or(existing, statusExpr);
                                                }
                                            }
                                            var assetGroupAttr = TraceCfgHelpers.FindNetworkAttribute(utilityNetworkDefinition, "Assetgroup", "Asset group");
                                            if (assetGroupAttr != null)
                                            {
                                                var assetGroupExpr = new NetworkAttributeComparison(assetGroupAttr, Operator.Equal, 51);
                                                var existing = sourceFuseCfg.Traversability.Barriers as ConditionalExpression;
                                                sourceFuseCfg.Traversability.Barriers = existing == null ? (Condition)assetGroupExpr : new Or(existing, assetGroupExpr);
                                            }

                                            TraceArgument sourceTraceArgument = new TraceArgument(new List<Element>() { sourceFuseElement });
                                            sourceTraceArgument.Configuration = sourceFuseCfg;
                                            IReadOnlyList<Result> sourceTraceResults = tracer.Trace(sourceTraceArgument);
                                            var sourceTraceExtract = new SpatialSubgraphExtractor(utilityNetwork).ExtractFromResults(sourceTraceResults);
                                            var transformer = sourceTraceExtract.FeatureByGlobalId.Values.FirstOrDefault(p => p.AssetGroupName == "Transformer");
                                            if (transformer == null)
                                            {
                                                return;
                                            }

                                            string txNo = transformer.GetString("TX_NO");
                                            string ssName = transformer.GetString("SSNAME");
                                            string ssNum = transformer.GetString("SSNUM");
                                            if (string.IsNullOrEmpty(ssName) || string.IsNullOrEmpty(ssNum))
                                            {
                                                var transformerAssociations = utilityNetwork.TraverseAssociations(new List<Element>() { transformer.Element }, new TraverseAssociationsDescription(TraversalDirection.Ascending));
                                                foreach (var transformerAssociation in transformerAssociations.Associations)
                                                {
                                                    if (transformerAssociation.FromElement.AssetGroup.Name == "Substation")
                                                    {
                                                        using (var substationTable = utilityNetwork.GetTable(transformerAssociation.FromElement.NetworkSource))
                                                        {
                                                            var substationFields = substationTable.GetDefinition().GetFields().Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                                                            using (var cursor = substationTable.Search(new QueryFilter() { ObjectIDs = new List<long>() { transformerAssociation.FromElement.ObjectID } }, false))
                                                            {
                                                                if (cursor.MoveNext())
                                                                {
                                                                    using (var row = cursor.Current)
                                                                    {
                                                                        if (string.IsNullOrEmpty(ssNum) && substationFields.Contains("SSNUM"))
                                                                        {
                                                                            ssNum = row["SSNUM"]?.ToString();
                                                                        }
                                                                        if (string.IsNullOrEmpty(ssName) && substationFields.Contains("SSNAME"))
                                                                        {
                                                                            ssName = row["SSNAME"]?.ToString();
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        break;
                                                    }
                                                }
                                            }

                                            var motherSupplyPoint = new LVFeature_Model(selectedFuseFeature, utilityNetwork)
                                            {
                                                POLENUM = poleFeature.GetString("POLENUM"),
                                                CCT_NO = sourceFuseFeature.GetString("CCT_NO"),
                                                TX_NO = txNo,
                                                SS_NAME = ssName,
                                                SS_NUM = ssNum,
                                                IsMotherSupplyPoint = true,
                                            };

                                            this.LVFeature = motherSupplyPoint;
                                            this.ShowLVMotherSupplyPointPanel = true;
                                        }
                                    }
                                }
                                else if (assetGroup == "LV Switch" && assetType == "Switch")
                                {
                                    var ascendingAssociations = utilityNetwork.TraverseAssociations(new List<Element>() { startElement }, new TraverseAssociationsDescription(TraversalDirection.Ascending));
                                    var descendingAssociations = utilityNetwork.TraverseAssociations(new List<Element>() { startElement }, new TraverseAssociationsDescription(TraversalDirection.Descending));
                                    var linkBoxElement = ascendingAssociations.Associations
                                        .Concat(descendingAssociations.Associations)
                                        .SelectMany(p => new List<Element>() { p.FromElement, p.ToElement })
                                        .FirstOrDefault(p => p.AssetType.Name == "Link Box");
                                    if (linkBoxElement == null)
                                    {
                                        return;
                                    }

                                    var linkBoxResults = new SpatialSubgraphExtractor(utilityNetwork).Extract(new List<Element>() { linkBoxElement });
                                    var linkBoxFeature = linkBoxResults.FeatureByGlobalId.Values.FirstOrDefault(p => p.Element.GlobalID == linkBoxElement.GlobalID);
                                    if (linkBoxFeature == null)
                                    {
                                        return;
                                    }

                                    DomainNetwork domainNetwork = utilityNetworkDefinition.GetDomainNetwork("Electric");
                                    Tier sourceTier = domainNetwork.GetTier("LV");
                                    LoggerHelper.Info($"Trace start at: {DateTime.Now}");
                                    TraceConfiguration cfg = sourceTier.GetTraceConfiguration();
                                    cfg.Propagators = new List<Propagator>();
                                    var catSwitch = utilityNetworkDefinition
                                        .GetAvailableCategories()
                                        .FirstOrDefault(c => c.Equals("E:Switch", StringComparison.OrdinalIgnoreCase));
                                    cfg.Filter.Scope = TraversabilityScope.JunctionsAndEdges;
                                    if (catSwitch != null)
                                    {
                                        var catExpr = new CategoryComparison(CategoryOperator.IsEqual, catSwitch);
                                        cfg.Traversability.Barriers = (Condition)catExpr;
                                    }

                                    var tcfg = startElement.AssetType.GetTerminalConfiguration();
                                    startElement.Terminal = tcfg.Terminals.FirstOrDefault(p => p.Name == "SS:S1");

                                    using (TraceManager traceManager = utilityNetwork.GetTraceManager())
                                    {
                                        TraceArgument traceArgument = new TraceArgument(new List<Element>() { startElement });
                                        traceArgument.Configuration = cfg;
                                        Tracer tracer = traceManager.GetTracer<ConnectedTracer>();
                                        IReadOnlyList<Result> traceResults = tracer.Trace(traceArgument);
                                        var results = new SpatialSubgraphExtractor(utilityNetwork).ExtractFromResults(traceResults);
                                        LoggerHelper.Info($"Trace end at: {DateTime.Now}");
                                        await HighlightPathOnMapAsync(utilityNetwork, results.FeatureByGlobalId.Values);

                                        var features = results.FeatureByGlobalId.Values;
                                        var supplyPointFeature = features.FirstOrDefault(p => p.AssetGroupName == "LV Service Point" && p.AssetTypeName == "Supply Point");
                                        if (supplyPointFeature == null)
                                        {
                                            return;
                                        }

                                        var lvSwitchFeatures = features
                                            .Where(p => p.AssetGroupName == "LV Switch" && p.AssetTypeName == "Switch")
                                            .ToList();

                                        var supplyPoint = new LVFeature_Model(supplyPointFeature, utilityNetwork);
                                        var linkBox = new LVFeature_Model(linkBoxFeature, utilityNetwork)
                                        {
                                            SPSID = supplyPoint.SPSID,
                                            ADDRESS = supplyPoint.ADDRESS,
                                            SUBNETWORKNAME = linkBoxFeature.GetString("SUPPORTEDSUBNETWORKNAME"),
                                        };

                                        LVFeature_Model CreateLVSwitchModel(FeatureSnapshot feature)
                                        {
                                            var model = new LVFeature_Model(feature, utilityNetwork);
                                            model.SPSID = supplyPoint.SPSID;
                                            model.ADDRESS = supplyPoint.ADDRESS;
                                            return model;
                                        }

                                        var lvSwitchModels = lvSwitchFeatures.Select(CreateLVSwitchModel).ToList();
                                        var selectedLVSwitch = lvSwitchModels.FirstOrDefault(p => p.Source.Element.GlobalID == startElement.GlobalID);

                                        this.LVFeature = selectedLVSwitch;
                                        this.LVFeatureContainer = new LVFeatureContainer_Model(null, null, null, null, null, null, lvSwitchModels, selectedLVSwitch, linkBox, supplyPoint);
                                        this.ShowLVLinkBoxPanel = true;
                                    }
                                }
                                this.ShowSearchPanel = false;
                                this.ShowLVFeaturePanel = true;
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
        private Pole_Model _poleDevice;
        private LVFeature_Model _lvFeature;
        private LVFeatureContainer_Model _lvFeatureContainer;

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

        public Pole_Model PoleDevice
        {
            get => _poleDevice;
            set => SetProperty(ref _poleDevice, value);
        }

        public LVFeature_Model LVFeature
        {
            get => _lvFeature;
            set => SetProperty(ref _lvFeature, value);
        }

        public LVFeatureContainer_Model LVFeatureContainer
        {
            get => _lvFeatureContainer;
            set => SetProperty(ref _lvFeatureContainer, value);
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
                        .Where(l => string.Equals((l.GetTable() != null) ? l.GetTable().GetName() : "", table.GetName(), StringComparison.OrdinalIgnoreCase));
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
            { ADMSUpdateMode.SS_TO_SS, "Update SS To SS" },
            { ADMSUpdateMode.SpareCB, "Update Spare CB" },
            { ADMSUpdateMode.Pole, "Manual Update Pole Feature" },
            { ADMSUpdateMode.PoleCable, "Multiple Update Pole Cable/OHL" },
            { ADMSUpdateMode.LVFeature, "Update LV Feature(Validating)" },
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

        public RelayCommand RefreshCommand { get; }
    }

    public enum ADMSUpdateMode
    {
        SS_TO_SS,
        SpareCB,
        Pole,
        PoleCable,
        LVFeature,
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