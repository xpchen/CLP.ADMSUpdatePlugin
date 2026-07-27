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
    public class Pole_Model : PropertyChangedBase
    {
        public string GLOBALID { get; set; }

        public string ASSET_TYPE { get; set; }

        private bool _showCircuitFields = true;
        public bool ShowCircuitFields
        {
            get => _showCircuitFields;
            set => SetProperty(ref _showCircuitFields, value);
        }

        private bool _showFromSubstationFields = true;
        public bool ShowFromSubstationFields
        {
            get => _showFromSubstationFields;
            set => SetProperty(ref _showFromSubstationFields, value);
        }

        private bool _showToSubstationFields = true;
        public bool ShowToSubstationFields
        {
            get => _showToSubstationFields;
            set => SetProperty(ref _showToSubstationFields, value);
        }

        private bool _showFromPoleNo = true;
        public bool ShowFromPoleNo
        {
            get => _showFromPoleNo;
            set => SetProperty(ref _showFromPoleNo, value);
        }

        private bool _showToPoleNo = true;
        public bool ShowToPoleNo
        {
            get => _showToPoleNo;
            set => SetProperty(ref _showToPoleNo, value);
        }

        private bool _showToPoleNoDropdown;
        public bool ShowToPoleNoDropdown
        {
            get => _showToPoleNoDropdown;
            set => SetProperty(ref _showToPoleNoDropdown, value);
        }

        public bool ShowToCircuitFields =>
            ASSET_TYPE == "Isolator" && !string.IsNullOrEmpty(TO_CIRCUIT_NAME) && TO_CIRCUIT_NAME != CIRCUIT_NAME;

        private List<FeatureSnapshot> _cableFeatures;
        public List<FeatureSnapshot> CableFeatures
        {
            get => _cableFeatures;
            set
            {
                SetProperty(ref _cableFeatures, value);
                NotifyPropertyChanged(nameof(ShowCableFields));
            }
        }

        public bool ShowCableFields => CableFeatures != null && CableFeatures.Count > 0;

        private PoleOptionList _toPoleNoOptions = new PoleOptionList();
        public PoleOptionList ToPoleNoOptions
        {
            get => _toPoleNoOptions;
            set
            {
                PoleOptionList filtered = new PoleOptionList();
                if (value != null)
                {
                    foreach (var poleNum in value.Where(p => !string.Equals(p, _from_pole_num, StringComparison.OrdinalIgnoreCase)))
                    {
                        filtered.Add(poleNum, value.GetCircuitName(poleNum), value.GetCircuitId(poleNum));
                    }
                }
                SetProperty(ref _toPoleNoOptions, filtered);
            }
        }

        private bool _showSOMFields = true;
        public bool ShowSOMFields
        {
            get => _showSOMFields;
            set => SetProperty(ref _showSOMFields, value);
        }

        private bool _showCheckBoxs = true;
        public bool ShowCheckBoxs
        {
            get => _showCheckBoxs;
            set => SetProperty(ref _showCheckBoxs, value);
        }

        private bool _enableCircuit = true;
        public bool EnableCircuit
        {
            get => _enableCircuit;
            set => SetProperty(ref _enableCircuit, value);
        }

        private bool _enableFromSubstation = true;
        public bool EnableFromSubstation
        {
            get => _enableFromSubstation;
            set => SetProperty(ref _enableFromSubstation, value);
        }

        private bool _enableToSubstation = true;
        public bool EnableToSubstation
        {
            get => _enableToSubstation;
            set => SetProperty(ref _enableToSubstation, value);
        }

        private void ResetFieldFlags()
        {
            ShowCircuitFields = true;
            ShowFromSubstationFields = true;
            ShowToSubstationFields = true;
            ShowFromPoleNo = true;
            ShowToPoleNo = true;
            ShowToPoleNoDropdown = false;
            ToPoleNoOptions = new PoleOptionList();
            ShowSOMFields = true;
            ShowCheckBoxs = false;
            EnableCircuit = true;
            EnableFromSubstation = true;
            EnableToSubstation = true;
        }

        private string _circuit_name;
        public string CIRCUIT_NAME 
        {
            get => _circuit_name; 
            set
            {
                SetProperty(ref _circuit_name, value);
                NotifyPropertyChanged(nameof(ShowToCircuitFields));
            }
        }

        private string _circuit_id;
        public string CIRCUIT_ID
        {
            get => _circuit_id;
            set => SetProperty(ref _circuit_id, value);
        }

        private string _from_pole_num;
        public string FROM_POLE_NUM
        {
            get => _from_pole_num;
            set => SetProperty(ref _from_pole_num, value);
        }

        private string _to_pole_num;
        public string TO_POLE_NUM
        {
            get => _to_pole_num;
            set
            {
                SetProperty(ref _to_pole_num, value);
                TO_CIRCUIT_NAME = _toPoleNoOptions.GetCircuitName(value);
                TO_CIRCUIT_ID = _toPoleNoOptions.GetCircuitId(value);
            }
        }

        private string _to_circuit_name;
        public string TO_CIRCUIT_NAME
        {
            get => _to_circuit_name;
            set
            {
                SetProperty(ref _to_circuit_name, value);
                NotifyPropertyChanged(nameof(ShowToCircuitFields));
            }
        }

        private string _to_circuit_id;
        public string TO_CIRCUIT_ID
        {
            get => _to_circuit_id;
            set => SetProperty(ref _to_circuit_id, value);
        }

        private string _from_ss_name;
        public string FROM_SS_NAME
        {
            get => _from_ss_name;
            set => SetProperty(ref _from_ss_name, value);
        }

        private string _from_ss_num;
        public string FROM_SS_NUM
        {
            get => _from_ss_num;
            set => SetProperty(ref _from_ss_num, value);
        }

        private string _to_ss_name;
        public string TO_SS_NAME
        {
            get => _to_ss_name;
            set => SetProperty(ref _to_ss_name, value);
        }

        private string _to_ss_num;
        public string TO_SS_NUM
        {
            get => _to_ss_num;
            set => SetProperty(ref _to_ss_num, value);
        }

        private bool _isTxOrPMSInPole;

        public bool IsTxOrPMSInPole
        {
            get => _isTxOrPMSInPole;
            set
            {
                SetProperty(ref _isTxOrPMSInPole, value);
                if (ASSET_TYPE == "Isolator") this.ShowFromSubstationFields = value;
                if (ASSET_TYPE == "HV PM TX") this.ShowToPoleNoDropdown = !value;
                if (ASSET_TYPE == "Fuse")
                {
                    this.ShowFromSubstationFields = value;
                    if (InPoleType != "PMS")
                    {
                        this.ShowToPoleNo = !value;
                        this.ShowToPoleNoDropdown = !value;
                    } 
                    else
                    {
                        this.ShowToPoleNo = true;
                        this.ShowToPoleNoDropdown = true;
                    }
                }
            }
        }

        private string _inPoleType;

        public string InPoleType
        {
            get => _inPoleType;
            set => SetProperty(ref _inPoleType, value);
        }

        private bool _isSingleDevice;

        public bool IsSingleDevice
        {
            get => _isSingleDevice;
            set => SetProperty(ref _isSingleDevice, value);
        }

        public bool IsTransformerOrSwitch
        {
            get
            {
                return this.ASSET_TYPE == "HV PM TX" || this.ASSET_TYPE == "Switch";
            }
        }

        public bool IsFuse
        {
            get
            {
                return this.ASSET_TYPE == "Fuse";
            }
        }

        public bool ShowPoleSOMFields
        {
            get
            {
                return !this.IsFuse;
            }
        }

        public string SOMSS
        {
            get
            {
                if (this.ASSET_TYPE == "Switch") return ADMSUpdateHelper.GetPMS_SOM_SS(this);
                //if (this.ASSET_TYPE == "HV PM TX") return ADMSUpdateHelper.ReplaceMultipleSpaces($"{this.FROM_SS_NAME?.Replace("S/S", "")}");
                if (this.ASSET_TYPE == "Isolator") return ADMSUpdateHelper.GetIsolator_SOM_SS(this);
                if (this.ASSET_TYPE == "Subring Circuit Breaker") return ADMSUpdateHelper.GetSubringCB_SOM_SS(this);
                if (this.ASSET_TYPE == "Fuse") return ADMSUpdateHelper.GetSOMSSForFuse(this);
                return "";
            }
        }

        public string SOMCCT
        {
            get
            {
                if (this.ASSET_TYPE == "Switch") return ADMSUpdateHelper.GetPMS_SOM_CCT(this);
                //if (this.ASSET_TYPE == "HV PM TX") return "P/M Tx";
                if (this.ASSET_TYPE == "Isolator") return ADMSUpdateHelper.GetIsolator_SOM_CCT(this);
                if (this.ASSET_TYPE == "Subring Circuit Breaker") return ADMSUpdateHelper.GetSubringCB_SOM_CCT(this);
                return "";
            }
        }

        public string ADMS_Name
        {
            get
            {
                if (this.ASSET_TYPE == "Isolator") return ADMSUpdateHelper.GetADMSNameForIsolator(this);
                if (this.ASSET_TYPE == "Fuse") return ADMSUpdateHelper.GetADMSNameForFuse(this);
                if (this.ASSET_TYPE == "HV PM TX") return ADMSUpdateHelper.GetADMSNameForTransformer(this);
                if (this.ASSET_TYPE == "Switch") return ADMSUpdateHelper.GetADMSNameForPMS(this);
                if (this.ASSET_TYPE == "Subring Circuit Breaker") return ADMSUpdateHelper.GetADMSNameForSubringCB(this);
                return "";
            }
        }

        public string ADMS_Alias
        {
            get
            {
                if (this.ASSET_TYPE == "Isolator") return ADMSUpdateHelper.GetADMSAliasForIsolator(this);
                if (this.ASSET_TYPE == "Fuse") return ADMSUpdateHelper.GetADMSAliasForFuse(this);
                if (this.ASSET_TYPE == "HV PM TX") return ADMSUpdateHelper.GetADMSAliasForTransformer(this);
                if (this.ASSET_TYPE == "Switch") return ADMSUpdateHelper.GetADMSAliasForPMS(this);
                if (this.ASSET_TYPE == "Subring Circuit Breaker") return ADMSUpdateHelper.GetADMSAliasForSubringCB(this);
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

        public void RefreshLabels()
        {
            NotifyPropertyChanged(nameof(ADMSNameLabel));
            NotifyPropertyChanged(nameof(ADMSAliasLabel));
            NotifyPropertyChanged(nameof(SOMSSLabel));
            NotifyPropertyChanged(nameof(SOMCCTLabel));
        }

        public FeatureSnapshot Source { get; set; }

        public FeatureSnapshot Pole { get; set; }

        private UtilityNetwork UtilityNetwork { get; set; }

        public Pole_Model(FeatureSnapshot source, UtilityNetwork utilityNetwork)
        {
            this.Source = source;
            this.UtilityNetwork = utilityNetwork;
            this.ResetFieldFlags();
            if (source.Attributes.ContainsKey("GLOBALID"))
            {
                this.GLOBALID = source.Attributes["GLOBALID"]?.ToString();
            }
            if (source.Attributes.ContainsKey("ASSETTYPE"))
            {
                this.ASSET_TYPE = source.AssetTypeName.ToString();
                if (this.ASSET_TYPE == "HV PM TX" || this.ASSET_TYPE == "Switch")
                {
                    this.ShowCircuitFields = false;
                    this.ShowFromPoleNo = false;
                    this.ShowToPoleNo = false;
                    this.ShowToSubstationFields = false;
                    this.ShowCheckBoxs = false;
                    this.ShowSOMFields = false;
                } 
                else if (this.ASSET_TYPE == "Fuse")
                {
                    this.ShowToPoleNo = false;
                    this.ShowToSubstationFields = false;
                }
                else if (this.ASSET_TYPE == "Subring Circuit Breaker")
                {
                    this.ShowFromPoleNo = false;
                    this.ShowToSubstationFields = false;
                    this.ShowToPoleNo = false;
                    this.ShowToPoleNoDropdown = false;
                    this.ShowCheckBoxs = false;
                }
            }
            if (source.Attributes.ContainsKey("CIRCUITNAME"))
            {
                this.CIRCUIT_NAME = source.Attributes["CIRCUITNAME"]?.ToString();
            }
            if (source.Attributes.ContainsKey("CIRCUITID"))
            {
                this.CIRCUIT_ID = source.Attributes["CIRCUITID"]?.ToString();
            }
            if (source.Attributes.ContainsKey("SSNAME"))
            {
                this.FROM_SS_NAME = source.Attributes["SSNAME"]?.ToString();
            }
            if (source.Attributes.ContainsKey("SSNUM"))
            {
                this.FROM_SS_NUM = source.Attributes["SSNUM"]?.ToString();
            }
        }
    }

    public class PoleOptionList : List<string>
    {
        private Dictionary<string, string> _circuitNames = new Dictionary<string, string>();
        private Dictionary<string, string> _circuitIds = new Dictionary<string, string>();

        public void Add(string poleNum, string circuitName, string circuitId)
        {
            if (!this.Contains(poleNum))
                base.Add(poleNum);
            if (circuitName != null)
                _circuitNames[poleNum] = circuitName;
            if (circuitId != null)
                _circuitIds[poleNum] = circuitId;
        }

        public string GetCircuitName(string poleNum)
        {
            return _circuitNames.TryGetValue(poleNum, out var name) ? name : null;
        }

        public string GetCircuitId(string poleNum)
        {
            return _circuitIds.TryGetValue(poleNum, out var id) ? id : null;
        }
    }
}
