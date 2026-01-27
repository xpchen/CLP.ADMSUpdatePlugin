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

        private string _circuit_name;
        public string CIRCUIT_NAME 
        {
            get => _circuit_name; 
            set => SetProperty(ref _circuit_name, value);
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
            set => SetProperty(ref _to_pole_num, value);
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

        private bool _isTxInPole;

        public bool IsTxInPole
        {
            get => _isTxInPole;
            set => SetProperty(ref _isTxInPole, value);
        }

        private bool _isSingleDevice;

        public bool IsSingleDevice
        {
            get => _isSingleDevice;
            set => SetProperty(ref _isSingleDevice, value);
        }

        public string ADMS_Name
        {
            get
            {
                if (this.ASSET_TYPE == "Isolator") return ADMSUpdateHelper.GetADMSNameForIsolator(this);
                if (this.ASSET_TYPE == "Fuse") return ADMSUpdateHelper.GetADMSNameForFuse(this);
                if (this.ASSET_TYPE == "HV PM TX") return ADMSUpdateHelper.GetADMSNameForTransformer(this);
                if (this.ASSET_TYPE == "Switch") return ADMSUpdateHelper.GetADMSNameForPMS(this);
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
                return "";
            }
        }

        public FeatureSnapshot Source { get; set; }

        public FeatureSnapshot Pole { get; set; }

        private UtilityNetwork UtilityNetwork { get; set; }

        public Pole_Model(FeatureSnapshot source, UtilityNetwork utilityNetwork)
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
                this.SS_NAME = source.Attributes["SSNAME"]?.ToString();
            }
            if (source.Attributes.ContainsKey("SSNUM"))
            {
                this.SS_NUM = source.Attributes["SSNUM"]?.ToString();
            }
        }
    }
}
