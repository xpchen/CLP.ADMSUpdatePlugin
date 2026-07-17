using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CLP.ADMSUpdatePlugin
{
    public class LVFeature_Model : PropertyChangedBase
    {
        public string GLOBALID { get; set; }

        public string ASSET_TYPE { get; set; }

        public string SUBNETWORKNAME { get; set; }

        private string _cct_no;
        public string CCT_NO
        {
            get => _cct_no;
            set => SetProperty(ref _cct_no, value);
        }

        private string _ss_name;
        public string SS_NAME
        {
            get => _ss_name;
            set => SetProperty(ref _ss_name, value);
        }

        private string _ss_num;
        public string SS_NUM
        {
            get => _ss_num;
            set => SetProperty(ref _ss_num, value);
        }

        private string _tx_no;
        public string TX_NO
        {
            get => _tx_no;
            set => SetProperty(ref _tx_no, value);
        }

        private string _pr_no;
        public string PR_NO
        {
            get => _pr_no;
            set => SetProperty(ref _pr_no, value);
        }

        private string _pr_name;
        public string PR_NAME
        {
            get => _pr_name;
            set => SetProperty(ref _pr_name, value);
        }

        private string _spsid;
        public string SPSID
        {
            get => _spsid;
            set => SetProperty(ref _spsid, value);
        }

        private string _address;
        public string ADDRESS
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private string _polenum;
        public string POLENUM
        {
            get => _polenum;
            set => SetProperty(ref _polenum, value);
        }

        private string _leg;
        public string LEG
        {
            get => _leg;
            set => SetProperty(ref _leg, value);
        }

        private bool _isPoleSourceFuse;
        public bool IsPoleSourceFuse
        {
            get => _isPoleSourceFuse;
            set => SetProperty(ref _isPoleSourceFuse, value);
        }

        private bool _isMotherSupplyPoint;
        public bool IsMotherSupplyPoint
        {
            get => _isMotherSupplyPoint;
            set => SetProperty(ref _isMotherSupplyPoint, value);
        }



        public string ADMS_Name
        {
            get
            {
                if (this.ASSET_TYPE == "Source Fuse")
                {
                    if (this.IsPoleSourceFuse) return ADMSUpdateHelper.GetADMSNameForPoleSourceFuse(this);
                    return ADMSUpdateHelper.GetADMSNameForSourceFuse(this);
                }
                if (this.ASSET_TYPE == "Local Supply" || this.ASSET_TYPE == "Local Supply Point")
                {
                    return ADMSUpdateHelper.GetADMSNameForLocalSupply(this);
                }
                if (this.ASSET_TYPE == "Supply Point")
                {
                    return ADMSUpdateHelper.GetADMSNameForSupplyPoint(this);
                }
                if (this.ASSET_TYPE == "Fuse")
                {
                    if (this.IsMotherSupplyPoint) return ADMSUpdateHelper.GetADMSNameForMotherSupplyPoint(this);
                    return ADMSUpdateHelper.GetADMSNameForPillarFuse(this);
                }
                if (this.ASSET_TYPE == "Pillar Circuit Box")
                {
                    return ADMSUpdateHelper.GetADMSNameForPillar(this);
                }
                if (this.ASSET_TYPE == "Switch")
                {
                    return ADMSUpdateHelper.GetADMSNameForLinkBoxLeg(this);
                }
                if (this.ASSET_TYPE == "Link Box")
                {
                    return ADMSUpdateHelper.GetADMSNameForLinkBox(this);
                }
                return "";
            }
        }

        public string ADMS_Alias
        {
            get
            {
                if (this.ASSET_TYPE == "Source Fuse")
                {
                    if (this.IsPoleSourceFuse) return ADMSUpdateHelper.GetADMSAliasForPoleSourceFuse(this);
                    return ADMSUpdateHelper.GetADMSAliasForSourceFuse(this);
                }
                if (this.ASSET_TYPE == "Local Supply" || this.ASSET_TYPE == "Local Supply Point")
                {
                    return ADMSUpdateHelper.GetADMSAliasForLocalSupply(this);
                }
                if (this.ASSET_TYPE == "Supply Point")
                {
                    return ADMSUpdateHelper.GetADMSAliasForSupplyPoint(this);
                }
                if (this.ASSET_TYPE == "Fuse")
                {
                    if (this.IsMotherSupplyPoint) return ADMSUpdateHelper.GetADMSAliasForMotherSupplyPoint(this);
                    return ADMSUpdateHelper.GetADMSAliasForPillarFuse(this);
                }
                if (this.ASSET_TYPE == "Pillar Circuit Box")
                {
                    return ADMSUpdateHelper.GetADMSAliasForPillar(this);
                }
                if (this.ASSET_TYPE == "Switch")
                {
                    return ADMSUpdateHelper.GetADMSAliasForLinkBoxLeg(this);
                }
                if (this.ASSET_TYPE == "Link Box")
                {
                    return ADMSUpdateHelper.GetADMSAliasForLinkBox(this);
                }
                return "";
            }
        }

        public string SOMSS
        {
            get
            {
                if (this.ASSET_TYPE == "Source Fuse")
                {
                    if (this.IsPoleSourceFuse) return ADMSUpdateHelper.GetSOMSSForPoleSourceFuse(this);
                    return ADMSUpdateHelper.GetSOMSSForSourceFuse(this);
                }
                if (this.ASSET_TYPE == "Local Supply")
                {
                    return "";
                }
                if (this.ASSET_TYPE == "Fuse")
                {
                    if (this.IsMotherSupplyPoint) return ADMSUpdateHelper.GetSOMSSForMotherSupplyPoint(this);
                    return ADMSUpdateHelper.GetSOMSSForPillarFuse(this);
                }
                if (this.ASSET_TYPE == "Switch")
                {
                    return ADMSUpdateHelper.GetSOMSSForLinkBoxLeg(this);
                }
                return "";
            }
        }

        public string SOMCCT
        {
            get
            {
                if (this.ASSET_TYPE == "Source Fuse")
                {
                    if (this.IsPoleSourceFuse) return ADMSUpdateHelper.GetSOMCCTForPoleSourceFuse(this);
                    return ADMSUpdateHelper.GetSOMCCTForSourceFuse(this);
                }
                if (this.ASSET_TYPE == "Local Supply")
                {
                    return "";
                }
                if (this.ASSET_TYPE == "Fuse")
                {
                    if (this.IsMotherSupplyPoint) return ADMSUpdateHelper.GetSOMCCTForMotherSupplyPoint(this);
                    return ADMSUpdateHelper.GetSOMCCTForPillarFuse(this);
                }
                if (this.ASSET_TYPE == "Switch")
                {
                    return ADMSUpdateHelper.GetSOMCCTForLinkBoxLeg(this);
                }
                return "";
            }
        }

        public string ADMSNameLabel =>
            this.ADMS_Name == (this.Source.Attributes.ContainsKey("ADMS_Name") ? this.Source.Attributes["ADMS_Name"]?.ToString() : null)
            ? "ADMS Name: (Same as current value)" : "ADMS Name:";

        public string ADMSAliasLabel =>
            this.ADMS_Alias == (this.Source.Attributes.ContainsKey("ADMS_Alias") ? this.Source.Attributes["ADMS_Alias"]?.ToString() : null)
            ? "ADMS Alias: (Same as current value)" : "ADMS Alias:";

        public string SOMSSLabel =>
            this.SOMSS == (this.Source.Attributes.ContainsKey("SOM_SS") ? this.Source.Attributes["SOM_SS"]?.ToString() : null)
            ? "SOMSS: (Same as current value)" : "SOMSS:";

        public string SOMCCTLabel =>
            this.SOMCCT == (this.Source.Attributes.ContainsKey("SOM_CCT") ? this.Source.Attributes["SOM_CCT"]?.ToString() : null)
            ? "SOMCCT: (Same as current value)" : "SOMCCT:";

        public FeatureSnapshot Source { get; set; }

        public FeatureSnapshot Container { get; set; }

        private UtilityNetwork UtilityNetwork { get; set; }

        public LVFeature_Model(FeatureSnapshot source, UtilityNetwork utilityNetwork)
        {
            this.Source = source;
            this.UtilityNetwork = utilityNetwork;
            if (source.Attributes.ContainsKey("GLOBALID"))
            {
                this.GLOBALID = source.Attributes["GLOBALID"]?.ToString();
            }
            if (source.Attributes.ContainsKey("ASSETTYPE"))
            {
                this.ASSET_TYPE = source.AssetTypeName.ToString();
            }
            if (source.Attributes.ContainsKey("SUBNETWORKNAME"))
            {
                this.SUBNETWORKNAME = source.Attributes["SUBNETWORKNAME"]?.ToString();
            }
            if (source.Attributes.ContainsKey("CCT_NO"))
            {
                this.CCT_NO = source.Attributes["CCT_NO"]?.ToString();
            }
            if (source.Attributes.ContainsKey("SSNAME"))
            {
                this.SS_NAME = source.Attributes["SSNAME"]?.ToString();
            }
            if (source.Attributes.ContainsKey("SSNUM"))
            {
                this.SS_NUM = source.Attributes["SSNUM"]?.ToString();
            }
            if (source.Attributes.ContainsKey("TX_NO"))
            {
                this.TX_NO = source.Attributes["TX_NO"]?.ToString();
            }
            if (source.Attributes.ContainsKey("PR_NO"))
            {
                this.PR_NO = source.Attributes["PR_NO"]?.ToString();
            }
            if (source.Attributes.ContainsKey("PR_NAME"))
            {
                this.PR_NAME = source.Attributes["PR_NAME"]?.ToString();
            }
            if (source.Attributes.ContainsKey("SPSID"))
            {
                this.SPSID = source.Attributes["SPSID"]?.ToString();
            }
            if (source.Attributes.ContainsKey("ADDRESS"))
            {
                this.ADDRESS = source.Attributes["ADDRESS"]?.ToString();
            }
            if (source.Attributes.ContainsKey("POLENUM"))
            {
                this.POLENUM = source.Attributes["POLENUM"]?.ToString();
            }
            if (source.Attributes.ContainsKey("LEG"))
            {
                this.LEG = source.Attributes["LEG"]?.ToString();
            }
        }
    }

    public class LVFeatureContainer_Model : PropertyChangedBase
    {
        private bool _updateAllSourceFuse = true;
        private bool _updateSelectedSourceFuse;
        private bool _updateLocalSupplyPoint = true;
        private bool _updateAllPillarFuse = true;
        private bool _updateSelectedPillarFuse;
        private bool _updatePillarCircuitBox = true;
        private bool _updateSupplyPoint = true;
        private bool _updateLinkBox = true;
        private bool _updateAllLVSwitch = true;
        private bool _updateSelectedLVSwitch;

        public LVFeatureContainer_Model(IEnumerable<LVFeature_Model> sourceFuses, IEnumerable<LVFeature_Model> localSupplyPoints, LVFeature_Model selectedSourceFuse,
            IEnumerable<LVFeature_Model> pillarFuses = null, LVFeature_Model selectedPillarFuse = null, LVFeature_Model pillarCircuitBox = null,
            IEnumerable<LVFeature_Model> lvSwitches = null, LVFeature_Model selectedLVSwitch = null, LVFeature_Model linkBox = null, LVFeature_Model supplyPoint = null)
        {
            this.SourceFuses = sourceFuses?.ToList() ?? new List<LVFeature_Model>();
            this.LocalSupplyPoints = localSupplyPoints?.ToList() ?? new List<LVFeature_Model>();
            this.SelectedSourceFuse = selectedSourceFuse;
            this.SourceFuseDisplay = CreateSourceFuseDisplay(this.SourceFuses.FirstOrDefault() ?? selectedSourceFuse);
            this.PillarFuses = pillarFuses?.ToList() ?? new List<LVFeature_Model>();
            this.SelectedPillarFuse = selectedPillarFuse;
            this.PillarCircuitBox = pillarCircuitBox;
            this.PillarFuseDisplay = CreatePillarFuseDisplay(this.PillarFuses.FirstOrDefault() ?? selectedPillarFuse);
            this.LVSwitches = lvSwitches?.ToList() ?? new List<LVFeature_Model>();
            this.SelectedLVSwitch = selectedLVSwitch;
            this.LVSwitchDisplay = CreateLVSwitchDisplay(this.LVSwitches.FirstOrDefault() ?? selectedLVSwitch);
            this.LinkBox = linkBox;
            this.SupplyPoint = supplyPoint;
        }

        public List<LVFeature_Model> SourceFuses { get; }

        public List<LVFeature_Model> LocalSupplyPoints { get; }

        public List<LVFeature_Model> PillarFuses { get; }

        public List<LVFeature_Model> LVSwitches { get; }

        public LVFeature_Model SelectedSourceFuse { get; }

        public LVFeature_Model SelectedPillarFuse { get; }

        public LVFeature_Model SelectedLVSwitch { get; }

        public LVFeature_Model SourceFuseDisplay { get; }

        public LVFeature_Model PillarFuseDisplay { get; }

        public LVFeature_Model LVSwitchDisplay { get; }

        public LVFeature_Model PillarCircuitBox { get; }

        public LVFeature_Model LinkBox { get; }

        public LVFeature_Model SupplyPoint { get; }

        public int SourceFuseCount => this.SourceFuses.Count;

        public int LocalSupplyPointCount => this.LocalSupplyPoints.Count;

        public int PillarFuseCount => this.PillarFuses.Count;

        public int LVSwitchCount => this.LVSwitches.Count;

        public bool HasLocalSupplyPoint => this.LocalSupplyPoints.Any();

        public bool HasPillarCircuitBox => this.PillarCircuitBox != null;

        public bool HasLinkBox => this.LinkBox != null;

        public bool HasSupplyPoint => this.SupplyPoint != null;

        public bool UpdateSupplyPoint
        {
            get => _updateSupplyPoint;
            set => SetProperty(ref _updateSupplyPoint, value);
        }

        public bool UpdateLinkBox
        {
            get => _updateLinkBox;
            set => SetProperty(ref _updateLinkBox, value);
        }

        public bool UpdatePillarCircuitBox
        {
            get => _updatePillarCircuitBox;
            set => SetProperty(ref _updatePillarCircuitBox, value);
        }

        public bool UpdateLocalSupplyPoint
        {
            get => _updateLocalSupplyPoint;
            set => SetProperty(ref _updateLocalSupplyPoint, value);
        }

        public bool UpdateAllSourceFuse
        {
            get => _updateAllSourceFuse;
            set
            {
                if (SetProperty(ref _updateAllSourceFuse, value) && value)
                {
                    UpdateSelectedSourceFuse = false;
                }
            }
        }

        public bool UpdateSelectedSourceFuse
        {
            get => _updateSelectedSourceFuse;
            set
            {
                if (SetProperty(ref _updateSelectedSourceFuse, value) && value)
                {
                    UpdateAllSourceFuse = false;
                }
            }
        }

        public bool UpdateAllPillarFuse
        {
            get => _updateAllPillarFuse;
            set
            {
                if (SetProperty(ref _updateAllPillarFuse, value) && value)
                {
                    UpdateSelectedPillarFuse = false;
                }
            }
        }

        public bool UpdateSelectedPillarFuse
        {
            get => _updateSelectedPillarFuse;
            set
            {
                if (SetProperty(ref _updateSelectedPillarFuse, value) && value)
                {
                    UpdateAllPillarFuse = false;
                }
            }
        }

        public bool UpdateAllLVSwitch
        {
            get => _updateAllLVSwitch;
            set
            {
                if (SetProperty(ref _updateAllLVSwitch, value) && value)
                {
                    UpdateSelectedLVSwitch = false;
                }
            }
        }

        public bool UpdateSelectedLVSwitch
        {
            get => _updateSelectedLVSwitch;
            set
            {
                if (SetProperty(ref _updateSelectedLVSwitch, value) && value)
                {
                    UpdateAllLVSwitch = false;
                }
            }
        }

        public IEnumerable<LVFeature_Model> SourceFusesToUpdate
        {
            get
            {
                if (this.UpdateSelectedSourceFuse && this.SelectedSourceFuse != null)
                {
                    return new List<LVFeature_Model>() { this.SelectedSourceFuse };
                }
                if (this.UpdateAllSourceFuse)
                {
                    return this.SourceFuses;
                }
                return new List<LVFeature_Model>();
            }
        }

        public IEnumerable<LVFeature_Model> PillarFusesToUpdate
        {
            get
            {
                if (this.UpdateSelectedPillarFuse && this.SelectedPillarFuse != null)
                {
                    return new List<LVFeature_Model>() { this.SelectedPillarFuse };
                }
                if (this.UpdateAllPillarFuse)
                {
                    return this.PillarFuses;
                }
                return new List<LVFeature_Model>();
            }
        }

        public IEnumerable<LVFeature_Model> LVSwitchesToUpdate
        {
            get
            {
                if (this.UpdateSelectedLVSwitch && this.SelectedLVSwitch != null)
                {
                    return new List<LVFeature_Model>() { this.SelectedLVSwitch };
                }
                if (this.UpdateAllLVSwitch)
                {
                    return this.LVSwitches;
                }
                return new List<LVFeature_Model>();
            }
        }

        private static LVFeature_Model CreateSourceFuseDisplay(LVFeature_Model sourceFuse)
        {
            if (sourceFuse == null)
            {
                return null;
            }

            return new LVFeature_Model(sourceFuse.Source, null)
            {
                SUBNETWORKNAME = sourceFuse.SUBNETWORKNAME,
                SS_NAME = sourceFuse.SS_NAME,
                SS_NUM = sourceFuse.SS_NUM,
                TX_NO = sourceFuse.TX_NO,
                CCT_NO = "X",
                IsPoleSourceFuse = sourceFuse.IsPoleSourceFuse,
            };
        }

        private static LVFeature_Model CreatePillarFuseDisplay(LVFeature_Model pillarFuse)
        {
            if (pillarFuse == null)
            {
                return null;
            }

            return new LVFeature_Model(pillarFuse.Source, null)
            {
                SUBNETWORKNAME = pillarFuse.SUBNETWORKNAME,
                CCT_NO = "X",
                PR_NO = pillarFuse.PR_NO,
                PR_NAME = pillarFuse.PR_NAME,
            };
        }

        private static LVFeature_Model CreateLVSwitchDisplay(LVFeature_Model lvSwitch)
        {
            if (lvSwitch == null)
            {
                return null;
            }

            return new LVFeature_Model(lvSwitch.Source, null)
            {
                SUBNETWORKNAME = lvSwitch.SUBNETWORKNAME,
                SPSID = lvSwitch.SPSID,
                ADDRESS = lvSwitch.ADDRESS,
                LEG = "X",
            };
        }
    }
}
