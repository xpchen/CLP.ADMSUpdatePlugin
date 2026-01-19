using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CLP.ADMSUpdatePlugin
{
    public class SS_TO_SS_Model : PropertyChangedBase
    {
        public string SSCODE { get; set; }

        public string SSNAME { get; set; }

        public string SERIALNUMBER { get; }

        public string BB_NUMBER { get; }

        public string PANEL_NO { get; }

        public string TX_NO { get; set; }

        // ADMSName logic (circuit breaker)
        public string ADMSName
        {
            get
            {
                //// Part 1: 26 characters (substation name)
                //string substationSource = this.SSNAME.Replace("S/S", "").PadRight(26);

                //// Part 2: 41 characters
                //string bbSourcePart = string.IsNullOrEmpty(BB_NUMBER) ? "" : $"BD {BB_NUMBER}-";

                //string substationTarget =(Target.SSNAME == this.SSNAME)?"": Target.SSNAME.Replace("S/S", "").PadRight(26);
                //string bbTargetPart = (Target.SSNAME == this.SSNAME) ? "" : string.IsNullOrEmpty(Target.BB_NUMBER) ? "" : $"BD {Target.BB_NUMBER} ";
                //string serialNumberPart = string.IsNullOrEmpty(SERIALNUMBER) ? "" : $"#{SERIALNUMBER}";

                //// Ensuring Part 2 is exactly 41 characters long
                //string part2 = $"{bbSourcePart}{substationTarget}{bbTargetPart}{serialNumberPart}".PadRight(41);

                //// Part 3: 13 characters (asset type abbreviation)
                //string assetTypeAbbreviation = AssetTypeAbbreviations.ContainsKey(this.Source.AssetTypeName)
                //                                ? AssetTypeAbbreviations[this.Source.AssetTypeName]
                //                                : "";

                //string part3 = $"{assetTypeAbbreviation} ".PadRight(13);

                //return $"{substationSource}{part2}{part3}";
                if (this.Source.AssetGroupName == "HV Switch" && this.Target!=null && this.Target.Source.AssetGroupName== "Transformer")
                {
                    return ADMSUpdateHelper.GetADMSNameForCBToTransformer(this, this.Target);
                }
                if (this.Source.AssetGroupName == "Transformer")
                {
                    return ADMSUpdateHelper.GetADMSNameForTransformer(this);
                }
                else
                {
                    return ADMSUpdateHelper.GetADMSNameForCBToCB(this, Target);
                }
            }
        }

        // ADMSAlias logic (circuit breaker)
        public string ADMSAlias
        {
            get
            {
                if (this.Source.AssetGroupName == "Transformer")
                {
                    return ADMSUpdateHelper.GetADMSAliasForTransformer(this);
                }
                else { 
                    return ADMSUpdateHelper.GetADMSAliasForCBToCB(this, Target);
                }
                //// Part 1: 7 characters (substation number)
                //string substationSource = this.SSCODE.PadRight(7);

                //// Part 2: 15 characters (PNL B{BB_NUMBER}/)
                //string bbSourcePart = string.IsNullOrEmpty(BB_NUMBER) ? "" : $" B{BB_NUMBER}/";
                //string panelPart = (String.IsNullOrEmpty( PANEL_NO)?"": PANEL_NO).PadRight(15 - bbSourcePart.Length);  // Ensuring total length is 15

                //// Part 3: 8 characters (asset type abbreviation)
                //string assetTypeAbbreviation = AssetTypeAbbreviations.ContainsKey(this.Source.AssetTypeName)
                //                                ? AssetTypeAbbreviations[this.Source.AssetTypeName]
                //                                : "";

                //string part3 = $"{assetTypeAbbreviation} ".PadRight(8);

                //return $"{substationSource}PNL {bbSourcePart}{panelPart} {part3}";
               
            }
        }

        // BusADMSName logic (busbar/busnode)
        public string BusADMSName
        {
            get; private set;
          
        }

        // BusADMSAlias logic (busbar/busnode)
        public string BusADMSAlias
        {
            get
            {
                return ADMSUpdateHelper.GetBusADMSAlias(this);
                //if (this.Busbar != null)
                //{
                //    // Part 1: 7 characters (substation number)
                //    string substationSource = this.SSCODE.PadRight(7);
                //    string sBB_NUMBER = this.Busbar.Attributes["BB_NUMBER"]?.ToString();
                //    // Part 2: 15 characters (BB number and "BB")
                //    string bbSourcePart = string.IsNullOrEmpty(sBB_NUMBER) ? "" : $" BD {sBB_NUMBER}";
                //    string bbPart = $" BB";
                //    string part2 = $"{bbSourcePart}{bbPart}".PadRight(15);

                //    // Part 3: 8 characters ("BB-SEGM")
                //    string part3 = "BB-SEGM ".PadRight(8);

                //    return $"{substationSource}{part2}{part3}";
                //}
                //return "";
            }
        }

        public FeatureSnapshot Source { get; set; }

        public SS_TO_SS_Model Target { get; set; }

        public FeatureSnapshot Substation { get; set; }

        private FeatureSnapshot _busbar;

        public FeatureSnapshot Busbar
        {
            get => _busbar;
            set => SetProperty(ref _busbar, value);
        }

        public FeatureSnapshot Transformer { get; set; }

        private UtilityNetwork UtilityNetwork { get; set; }

        public SS_TO_SS_Model(FeatureSnapshot source,UtilityNetwork 
            utilityNetwork)
        {
            this.Source = source;
            this.UtilityNetwork = utilityNetwork;
            if (source.Attributes.ContainsKey("SERIALNUMBER"))
            {
                this.SERIALNUMBER = source.Attributes["SERIALNUMBER"]?.ToString();
            }
            if (source.Attributes.ContainsKey("PANEL_NO"))
            {
                this.PANEL_NO = source.Attributes["PANEL_NO"]?.ToString();
            }
            if (source.Attributes.ContainsKey("BB_NUMBER"))
            {
                this.BB_NUMBER = source.Attributes["BB_NUMBER"]?.ToString();
            }
            if (source.Attributes.ContainsKey("TX_NO"))
            {
                this.TX_NO = source.Attributes["TX_NO"]?.ToString();
            }

            this.PropertyChanged += SS_TO_SS_Model_PropertyChanged;
        }

        private async void SS_TO_SS_Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Busbar":
                    {
                        this.BusADMSName = await ADMSUpdateHelper.GetBusADMSName(this.UtilityNetwork, this);
                    }
                    break;
                default:
                    break;
            }
        }

        private bool _isChecked;

        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        public SS_TO_SS_ResultType ResultType { get; set; }
    }

    public enum SS_TO_SS_ResultType
    {
        CB_TO_CB = 0,
        CB_TO_SCB = 1,
        CB_TO_TRANSFORMER = 2
    }
}
