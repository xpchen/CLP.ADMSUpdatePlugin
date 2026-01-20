using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Editing.COGO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CLP.ADMSUpdatePlugin
{
    public class ADMSUpdateHelper
    {
        public static string ReplaceMultipleSpaces(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // 替换超过2个空格的部分为空
            return Regex.Replace(input, @"\s{2,}", " ").TrimEnd();
        }

        public static string GetCableADMSName(SS_TO_SS_Model src, SS_TO_SS_Model des, FeatureSnapshot cable, bool isTemplate=false)
        {
            string textA = ReplaceMultipleSpaces(src.SSNAME.Replace("S/S", ""));
            if (!String.IsNullOrEmpty(src.BB_NUMBER))
            {
                textA += $" BD {src.BB_NUMBER}";
            }
            textA = ReplaceMultipleSpaces(textA);
            if (textA.Length < 25)
            {
                textA = textA.PadRight(25); // Pad the string to 25 characters
            }
            string textB = ReplaceMultipleSpaces(des.SSNAME.Replace("S/S", ""));
            if (des.Source.AssetGroupName == "Transformer")
            {
                string txPart = String.IsNullOrEmpty(des.TX_NO) ? "" : $" D{des.TX_NO}";
                textB += $" Tx{txPart}";
            } 
            if (!String.IsNullOrEmpty(des.BB_NUMBER) && des.Source.AssetGroupName != "Transformer")
            {
                textB += $" BD {des.BB_NUMBER}";
            }
            textB = ReplaceMultipleSpaces(textB);
            string textC = "";
            if (isTemplate)
            {
                textC = " LINE_" + "".PadRight(6, 'X'); // Use the objectid from the cable
            }
            else {
                textC = " LINE_" + cable.ObjectID; // Use the objectid from the cable
            }
            if (textC.Length < 13)
            {
                textC = textC.PadRight(13); // Pad the string to 13 characters
            }
            string combinedAB = textA + "-" + textB;
            string combined = combinedAB + textC;
            
            int totalLength = combined.Length;

            if (totalLength < 80)
            {
                int spaceCount = 80 - totalLength;
                string spacer = new string(' ', spaceCount); // Create the required number of spaces
                return combinedAB + spacer + textC;
            }
            else
            {
                return combined;
            }
        }

        // GetCableADMSAlias method (using srcSubstation, desSubstation, cable)
        public static string GetCableADMSAlias(SS_TO_SS_Model src, SS_TO_SS_Model des, FeatureSnapshot cable,bool isTemplate = false)
        {
            string textA = src.SSCODE;
            if (!String.IsNullOrEmpty(src.BB_NUMBER))
            {
                textA += $" B{src.BB_NUMBER}";
            }
            string textB = des.SSCODE;
            if (des.Source.AssetGroupName == "Transformer")
            {
                textB += String.IsNullOrEmpty(des.TX_NO) ? " D1" : $" D{des.TX_NO}";
            }
            if (!String.IsNullOrEmpty(des.BB_NUMBER) && des.Source.AssetGroupName != "Transformer")
            {
                textB += $" B{des.BB_NUMBER}";
            }
            string textC = isTemplate? "L" + "".PadRight(6,'X'): " L" + cable.ObjectID; // Use the objectid from the cable
            if (textC.Length < 9)
            {
                textC = textC.PadRight(9); // Pad the string to 9 characters
            }
            string combinedAB = textA + "-" + textB;
            string combined = combinedAB + textC;
            int totalLength = combined.Length;
            if (totalLength < 30)
            {
                int spaceCount = 30 - totalLength;
                string spacer = new string(' ', spaceCount); // Create the required number of spaces
                return combinedAB  + spacer + textC;
            }
            else
            {
                return combined;
            }
        }

        public static async Task<string> GetBusADMSName(UtilityNetwork un, SS_TO_SS_Model model)
        {
            if (model?.Busbar != null)
            {
                // Run the task asynchronously on the QueuedTask thread and return the result
                return await QueuedTask.Run(() =>
                {
                    // Part 1: 26 characters (substation name)
                    string part1 = ReplaceMultipleSpaces(model.SSNAME.Replace("S/S", "")).PadRight(26);
                    // Get the switch table and the complex switch model field
                    var switchTable = un.GetTable(model.Source.Element.NetworkSource);
                    var fld_complex_switch_model = switchTable.GetDefinition().GetFields()
                        .FirstOrDefault(p => "complex_switch_model".Equals(p.Name, StringComparison.OrdinalIgnoreCase));
                    var fld_complex_switch_model_domain = fld_complex_switch_model?.GetDomain() as CodedValueDomain;
                    var complexSwitchModelName = fld_complex_switch_model_domain?.GetName(model.Source.Attributes["complex_switch_model"]);
                    // Part 2: 41 characters (complex switch model and BB number)
                    //string complexSwitchModel = model.Source.Attributes["complex_switch_model"]?.ToString() ?? "";
                    string sBB_NUMBER = model.Busbar.Attributes["BB_NUMBER"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(sBB_NUMBER))
                    {
                        sBB_NUMBER = model.BB_NUMBER;
                    }
                    string bbSourcePart = string.IsNullOrEmpty(sBB_NUMBER) ? "" : $" BD {sBB_NUMBER}";
                    string part2 = ReplaceMultipleSpaces($"{complexSwitchModelName}{bbSourcePart}").PadRight(41);
                    // Part 3: 13 characters ("BB-SEGM")
                    string part3 = "BB-SEGM".PadRight(13);
                    // Return the concatenated result
                    return $"{part1}{part2}{part3}";
                });
            }

            return string.Empty;
        }


        // BusADMSAlias logic (busbar/busnode)
        public static string GetBusADMSAlias(SS_TO_SS_Model model)
        {

            if (model.Busbar != null)
            {
                // Part 1: 7 characters (substation number)
                string substationSource = model.SSCODE.PadRight(7);
                string sBB_NUMBER = model.Busbar.Attributes["BB_NUMBER"]?.ToString();
                if (string.IsNullOrEmpty(sBB_NUMBER))
                {
                    sBB_NUMBER = model.BB_NUMBER;
                }
                // Part 2: 15 characters (BB number and "BB")
                string bbSourcePart = string.IsNullOrEmpty(sBB_NUMBER) ? "" : $"BD {sBB_NUMBER}";
                string bbPart = $" BB";
                string part2 = $"{bbSourcePart}{bbPart}".PadRight(15);

                // Part 3: 8 characters ("BB-SEGM")
                string part3 = "BB-SEGM ".PadRight(8);

                return $"{substationSource}{part2}{part3}";
            }
            return "";
        }

        public static string GetCB_SOM_SS(SS_TO_SS_Model first)
        {
            string substationSource = ReplaceMultipleSpaces(first.SSNAME.Replace("S/S", ""));
            string bbSourcePart = string.IsNullOrEmpty(first.BB_NUMBER) ? "" : $"BD {first.BB_NUMBER}";
            return $"{substationSource} {bbSourcePart}";
        }

        public static string GetCB_SOM_CCT(SS_TO_SS_Model first, SS_TO_SS_Model second)
        {
            string substationTarget = ReplaceMultipleSpaces(second.SSNAME.Replace("S/S", ""));
            string bbTargetPart = string.IsNullOrEmpty(second.BB_NUMBER) ? "" : $" BD {second.BB_NUMBER} ";
            string serialNumberPart = string.IsNullOrEmpty(first.SERIALNUMBER) ? "" : $" #{first.SERIALNUMBER}";
            string part2 = $"{substationTarget}{bbTargetPart}{serialNumberPart}";
            if (second.Source.AssetGroupName == "Transformer")
            {
                string txPart = string.IsNullOrEmpty(second.TX_NO) ? "" : $" D{second.TX_NO}";
                if (first.SSCODE == second.SSCODE)
                    part2 = $"L/Tx{txPart}";
                else
                    part2 = $"{substationTarget} Tx {txPart}";
            }
            return ReplaceMultipleSpaces(part2);
        }

        public static string GetADMSNameForCBToCB(SS_TO_SS_Model first, SS_TO_SS_Model second)
        {
            // ADMS Name for CB to CB
            string substationSource = ReplaceMultipleSpaces(first.SSNAME.Replace("S/S", "")).PadRight(26);
            string bbSourcePart = string.IsNullOrEmpty(first.BB_NUMBER) ? "" : $"BD {first.BB_NUMBER}-";
            string substationTarget = ReplaceMultipleSpaces(second.SSNAME.Replace("S/S", ""));
            string bbTargetPart = "";
            if (first.SSCODE == second.SSCODE)
            {
                substationTarget = "";
                bbTargetPart = string.IsNullOrEmpty(second.BB_NUMBER) ? "" : $"BD {second.BB_NUMBER} ";
            }
            else {
                bbTargetPart = string.IsNullOrEmpty(second.BB_NUMBER) ? "" : $" BD {second.BB_NUMBER} ";
            }
                
            string serialNumberPart = string.IsNullOrEmpty(first.SERIALNUMBER) ? "" : $" #{first.SERIALNUMBER}";
            string part2 = $"{bbSourcePart}{substationTarget}{bbTargetPart}{serialNumberPart}".PadRight(41);
            string assetTypeAbbreviation = AssetTypeAbbreviations.ContainsKey(first.Source.AssetTypeName)
                                            ? AssetTypeAbbreviations[first.Source.AssetTypeName]
                                            : "";
            string part3 = $"{assetTypeAbbreviation} ".PadRight(13);
            if (first.Source.AssetTypeName == "Source Circuit Breaker")
            {
                part3 = $"CBD{first.PANEL_NO ?? ""}5";
            }
            return $"{substationSource}{part2}{part3}";
        }

        private static readonly Dictionary<string, string> AssetTypeAbbreviations = new Dictionary<string, string>
        {
            { "Circuit Breaker", "CB" },
            { "Source Circuit Breaker", "SCB" }
        };

        public static string GetADMSAliasForCBToCB(SS_TO_SS_Model first, SS_TO_SS_Model second)
        {
            // ADMS Alias for CB to CB
            string substationSource = first.SSCODE.PadRight(7);
            string bbSourcePart = string.IsNullOrEmpty(first.BB_NUMBER) ? "" : $"B{first.BB_NUMBER}/";
            string panelPart = (string.IsNullOrEmpty(first.PANEL_NO) ? "" : first.PANEL_NO);
            string assetTypeAbbreviation = AssetTypeAbbreviations.ContainsKey(first.Source.AssetTypeName)
                                            ? AssetTypeAbbreviations[first.Source.AssetTypeName]
                                            : "";
            string part2 = $"PNL {bbSourcePart}{panelPart}".PadRight(15);
            string part3 = $"{assetTypeAbbreviation} ".PadRight(8);
            if (first.Source.AssetTypeName == "Source Circuit Breaker")
            {
                part3 = $"CB".PadRight(8);
            }

            return $"{substationSource}{part2}{part3}";
        }
        //两个以上的空格remove掉

        public static string GetADMSNameForCBToTransformer(SS_TO_SS_Model first, SS_TO_SS_Model second)
        {
            // ADMS Name for CB to Transformer
            string substationSource = ReplaceMultipleSpaces(first.SSNAME.Replace("S/S", "")).PadRight(26);
            string bbSourcePart = string.IsNullOrEmpty(first.BB_NUMBER) ? "" : $"BD {first.BB_NUMBER}-";
            string txPart = "";
            if (first.SSNAME != second.SSNAME) // If not in the same substation
            {
                txPart = $"{ReplaceMultipleSpaces(second.SSNAME.Replace("S/S", ""))} Tx ";
                if (!string.IsNullOrEmpty(second.TX_NO))
                {
                    txPart += $"D{second.TX_NO}";
                }
            }
            else
            {
                txPart = "L/Tx ";
                if (!string.IsNullOrEmpty(second.TX_NO))
                {
                    txPart += $"D{second.TX_NO}";
                }
            }
            string part2 = ReplaceMultipleSpaces($"{bbSourcePart}{txPart}").PadRight(41);
            string assetTypeAbbreviation ="CB";
            //SCB CB
            string part3 = $"{assetTypeAbbreviation}".PadRight(13);

            return $"{substationSource}{part2}{part3}";
        }

        public static string GetADMSNameForTransformer(SS_TO_SS_Model first)
        {
            // Part 1: substation_name(source) (如果有S/S 就替换为 "") + 空格
            string substationSource = ReplaceMultipleSpaces(first.SSNAME.Replace("S/S", "")).PadRight(26);

            // Part 2: "Tx " + transformer_number != null ? "D{transformer_number}" : "" + 空格
            string transformerPart = string.IsNullOrEmpty(first.TX_NO) ? "" : $"D{first.TX_NO}";
            string part2 = $"Tx {transformerPart}".PadRight(41);

            // Part 3: "LOAD" + 空格
            string part3 = "LOAD".PadRight(13);

            return $"{substationSource}{part2}{part3}";
        }

        public static string GetADMSAliasForTransformer(SS_TO_SS_Model first)
        {
            // ADMS Alias for Transformer
            string substationSource = first.SSCODE.PadRight(7);
            string transformerPart = string.IsNullOrEmpty(first.TX_NO) ? "D1" : $"D{first.TX_NO}";
            string part2 = $"{transformerPart}".PadRight(15);
            string part3 = "LOAD".PadRight(8);
            return $"{substationSource}{part2}{part3}";
        }
    }

}
