using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Desktop.Framework.Threading.Tasks;
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
        static int CompareHierarchy(string x, string y)
        {
            var p1 = x.Split('/');
            var p2 = y.Split('/');

            for (int i = 0; i < Math.Min(p1.Length, p2.Length); i++)
            {
                bool isN1 = int.TryParse(p1[i], out int n1);
                bool isN2 = int.TryParse(p2[i], out int n2);

                int cmp = (isN1, isN2) switch
                {
                    (true, true) => n1.CompareTo(n2),
                    (true, false) => -1, // Numbers before letters
                    (false, true) => 1,  // Letters after numbers
                    (false, false) => string.Compare(p1[i], p2[i], StringComparison.OrdinalIgnoreCase)
                };

                if (cmp != 0) return cmp;
            }

            // Shorter path (parent) comes first (e.g., "9" before "9/1")
            return p1.Length.CompareTo(p2.Length);
        }
        public static string HandlePrimarySubstationName(SS_TO_SS_Model first)
        {
            string scadaCode = ReplaceMultipleSpaces(first.Substation.Attributes["SCADACODE"]?.ToString());
            if (!string.IsNullOrEmpty(scadaCode) 
                && scadaCode.Length == 3)
                return $"{scadaCode}011";
            else if (first.SSNAME.Replace("S/S", "").Split(" ")[0]?.Length == 3)
                return $"{ReplaceMultipleSpaces(first.SSNAME.Replace("S/S", "").Split(" ")[0])}011";
            else return $"{ReplaceMultipleSpaces(first.SSNAME.Replace("S/S", ""))}";
        }
        public static string ReplaceMultipleSpaces(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // 替换超过2个空格的部分为空
            return Regex.Replace(input, @"\s{2,}", " ").TrimEnd();
        }

        public static string GetCableADMSName(SS_TO_SS_Model src, SS_TO_SS_Model des, FeatureSnapshot cable, bool isTemplate=false)
        {
            string textA = src.SSNAME.Replace("S/S", "");
            if (src.Source.AssetTypeName == "Source Circuit Breaker")
                textA = HandlePrimarySubstationName(src);
            if (!String.IsNullOrEmpty(src.BB_NUMBER))
            {
                textA += $" BD {src.BB_NUMBER}";
            }
            textA = ReplaceMultipleSpaces(textA).ToFixedLength(25);
            string textB = des.SSNAME.Replace("S/S", "");
            if (des.Source.AssetTypeName == "Source Circuit Breaker")
                textB = HandlePrimarySubstationName(des);
            if (des.Source.AssetGroupName == "Transformer")
            {
                string txPart = String.IsNullOrEmpty(des.TX_NO) ? "" : $" D{des.TX_NO}";
                textB += $" Tx{txPart}";
            }
            if (!String.IsNullOrEmpty(src.SERIALNUMBER))
            {
                textB += $" #{src.SERIALNUMBER}";
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
            textC = textC.ToFixedLength(13);
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
                //if (!string.IsNullOrEmpty(des.TX_NO)
                //    && int.TryParse(des.TX_NO, out int tx_integer)
                //    && int.Parse(des.TX_NO) >= 10)
                //    textB += $" D{tx_integer.ToString("X")}";
                //else 
                //    textB += String.IsNullOrEmpty(des.TX_NO) ? " D1" : $" D{des.TX_NO}";
            }
            if (!String.IsNullOrEmpty(des.BB_NUMBER) && des.Source.AssetGroupName != "Transformer")
            {
                textB += $" B{des.BB_NUMBER}";
            }
            if (!String.IsNullOrEmpty(src.SERIALNUMBER))
            {
                textB += $" {src.SERIALNUMBER}";
            }
            string textC = isTemplate? "L" + "".ToFixedLength(6, 'X'): " L" + cable.ObjectID; // Use the objectid from the cable
            textC = textC.ToFixedLength(9);
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
                    string part1 = ReplaceMultipleSpaces(model.SSNAME.Replace("S/S", "")).ToFixedLength(26);
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
                    string part2 = ReplaceMultipleSpaces($"{complexSwitchModelName}{bbSourcePart}").ToFixedLength(41);
                    // Part 3: 13 characters ("BB-SEGM")
                    string part3 = "BB-SEGM".ToFixedLength(13);
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
                string substationSource = model.SSCODE.ToFixedLength(7);
                string sBB_NUMBER = model.Busbar.Attributes["BB_NUMBER"]?.ToString();
                if (string.IsNullOrEmpty(sBB_NUMBER))
                {
                    sBB_NUMBER = model.BB_NUMBER;
                }
                // Part 2: 15 characters (BB number and "BB")
                string bbSourcePart = string.IsNullOrEmpty(sBB_NUMBER) ? "" : $"BD {sBB_NUMBER}";
                string bbPart = $" BB";
                string part2 = $"{bbSourcePart}{bbPart}".ToFixedLength(15);

                // Part 3: 8 characters ("BB-SEGM")
                string part3 = "BB-SEGM ".ToFixedLength(8);

                return $"{substationSource}{part2}{part3}";
            }
            return "";
        }

        public static string GetCB_SOM_SS(SS_TO_SS_Model first)
        {
            string substationSource = first.SSNAME.Replace("S/S", "");
            if (first.Source.AssetTypeName == "Source Circuit Breaker")
                substationSource = HandlePrimarySubstationName(first).ToFixedLength(26);
            string bbSourcePart = string.IsNullOrEmpty(first.BB_NUMBER) ? "" : $" BD {first.BB_NUMBER}";
            return ReplaceMultipleSpaces($"{substationSource}{bbSourcePart}");
        }

        public static string GetCB_SOM_CCT(SS_TO_SS_Model first, SS_TO_SS_Model second)
        {
            string substationTarget = ReplaceMultipleSpaces(second.SSNAME.Replace("S/S", ""));
            if (second.Source.AssetTypeName == "Source Circuit Breaker")
                substationTarget = HandlePrimarySubstationName(second).ToFixedLength(26);
            string bbTargetPart = string.IsNullOrEmpty(second.BB_NUMBER) ? "" : $" BD {second.BB_NUMBER} ";
            string serialNumberPart = string.IsNullOrEmpty(first.SERIALNUMBER) ? "" : $" #{first.SERIALNUMBER}";
            var panelPart = "";
            if (first.Source.AssetTypeName == "Source Circuit Breaker")
                panelPart = $" (Panel {first.PANEL_NO})";
            string part2 = $"{substationTarget}{bbTargetPart}{serialNumberPart}{panelPart}";
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

        public static string GetCable_Terminal_Substation(SS_TO_SS_Model first, SS_TO_SS_Model second)
        {
            return ReplaceMultipleSpaces($"{GetCB_SOM_SS(first)} - {GetCB_SOM_CCT(first, second)}");
        }

        public static string GetSpare_CB_SOM_CCT(SS_TO_SS_Model first)
        {
            string bbPart = string.IsNullOrEmpty(first.BB_NUMBER) ? "" : $"B{first.BB_NUMBER}/";
            return $"SPARE (PNL {bbPart}{first.PANEL_NO})";
        }
        
        public static string GetIsolator_SOM_SS(Pole_Model first)
        {
            if (first.IsSingleDevice) return $"{first.CIRCUIT_NAME}";
            return ReplaceMultipleSpaces($"{first.CIRCUIT_NAME} P.{first.FROM_POLE_NUM}");
        }

        public static string GetIsolator_SOM_CCT(Pole_Model first)
        {
            if (first.IsSingleDevice) return ReplaceMultipleSpaces($"P.{first.FROM_POLE_NUM}");
            else if (!first.IsSingleDevice && string.IsNullOrEmpty(first.TO_POLE_NUM)) return ReplaceMultipleSpaces($"{first.TO_SS_NAME?.Replace("S/S", "")}");
            else if (!first.IsSingleDevice 
                && ReplaceMultipleSpaces(first.CIRCUIT_NAME) == ReplaceMultipleSpaces(first.TO_CIRCUIT_NAME) 
                && !string.IsNullOrEmpty(first.TO_POLE_NUM)) 
                return ReplaceMultipleSpaces($"P.{first.TO_POLE_NUM}");
            return ReplaceMultipleSpaces($"{first.TO_CIRCUIT_NAME} P.{first.TO_POLE_NUM}");
        }

        public static string GetPMS_SOM_SS(Pole_Model first)
        {
            if (first.IsSingleDevice) return ReplaceMultipleSpaces($"{first.CIRCUIT_NAME}");
            return ReplaceMultipleSpaces($"{first.CIRCUIT_NAME} ({first.FROM_SS_NAME})");
        }

        public static string GetPMS_SOM_CCT(Pole_Model first)
        {
            return $"{first.FROM_POLE_NUM}";
        }

        public static string GetRecloser_SOM_SS(Pole_Model first)
        {
            return ReplaceMultipleSpaces($"{first.CIRCUIT_NAME} ({first.FROM_SS_NAME?.Replace("S/S", "")})");
        }

        public static string GetRecloser_SOM_CCT(Pole_Model first)
        {
            return $"P.{first.FROM_POLE_NUM}";
        }

        public static string GetSubringCB_SOM_SS(Pole_Model first)
        {
            return ReplaceMultipleSpaces($"{first.FROM_SS_NAME.Replace("S/S", "")}");
        }

        public static string GetSubringCB_SOM_CCT(Pole_Model first)
        {
            return ReplaceMultipleSpaces($"{first.CIRCUIT_NAME} P{first.TO_POLE_NUM}");
        }
        public static string GetADMSNameForCBToCB(SS_TO_SS_Model first, SS_TO_SS_Model second)
        {
            // ADMS Name for CB to CB
            string substationSource = ReplaceMultipleSpaces(first.SSNAME.Replace("S/S", "")).ToFixedLength(26);
            if (first.Source.AssetTypeName == "Source Circuit Breaker")
                substationSource = HandlePrimarySubstationName(first).ToFixedLength(26);
            string bbSourcePart = string.IsNullOrEmpty(first.BB_NUMBER) ? "" : $"BD {first.BB_NUMBER}-";
            string substationTarget = ReplaceMultipleSpaces(second.SSNAME.Replace("S/S", ""));
            if (second.Source.AssetTypeName == "Source Circuit Breaker")
                substationTarget = HandlePrimarySubstationName(second).ToFixedLength(26);
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
            string part2 = $"{bbSourcePart}{substationTarget}{bbTargetPart}{serialNumberPart}".ToFixedLength(41);
            string assetTypeAbbreviation = AssetTypeAbbreviations.ContainsKey(first.Source.AssetTypeName)
                                            ? AssetTypeAbbreviations[first.Source.AssetTypeName]
                                            : "";
            string part3 = $"{assetTypeAbbreviation} ".ToFixedLength(13);
            if (first.Source.AssetTypeName == "Source Circuit Breaker")
                part3 = $"CBD{first.PANEL_NO ?? ""}5".ToFixedLength(13);
            return $"{substationSource}{part2}{part3}";
        }

        public static string GetADMSNameForSpareCB(SS_TO_SS_Model first)
        {
            string substationSource = ReplaceMultipleSpaces(first.SSNAME.Replace("S/S", "")).ToFixedLength(26);
            if (first.Source.AssetTypeName == "Source Circuit Breaker")
                substationSource = HandlePrimarySubstationName(first).ToFixedLength(26);
            string bbPart = string.IsNullOrEmpty(first.BB_NUMBER) ? "" : $"B{first.BB_NUMBER}/";
            string part2 = $"SPARE (PNL {bbPart}{first.PANEL_NO})".ToFixedLength(41);
            string assetTypeAbbreviation = AssetTypeAbbreviations.ContainsKey(first.Source.AssetTypeName)
                                            ? AssetTypeAbbreviations[first.Source.AssetTypeName]
                                            : "";
            string part3 = $"{assetTypeAbbreviation} ".ToFixedLength(13);
            if (first.Source.AssetTypeName == "Source Circuit Breaker")
                part3 = $"CBD{first.PANEL_NO ?? ""}5".ToFixedLength(13);
            return $"{substationSource}{part2}{part3}";
        }

        private static readonly Dictionary<string, string> AssetTypeAbbreviations = new Dictionary<string, string>
        {
            { "Circuit Breaker", "CB" },
            { "Source Circuit Breaker", "CB" },
            { "Switch", "LBS" },
        };

        public static string GetADMSAliasForCBToCB(SS_TO_SS_Model first, SS_TO_SS_Model second)
        {
            // ADMS Alias for CB to CB
            string substationSource = first.SSCODE.ToFixedLength(7);
            string bbSourcePart = string.IsNullOrEmpty(first.BB_NUMBER) ? "" : (first.BB_NUMBER.Contains("M") ? $"/{first.BB_NUMBER}" : $"B{first.BB_NUMBER}/");
            string panelPart = (string.IsNullOrEmpty(first.PANEL_NO) ? "" : first.PANEL_NO);
            string assetTypeAbbreviation = AssetTypeAbbreviations.ContainsKey(first.Source.AssetTypeName)
                                            ? AssetTypeAbbreviations[first.Source.AssetTypeName]
                                            : "";
            string panelUnit = "PNL";
            if (first.SSNAME.Contains("CUST EQPT"))
                panelUnit = "BAY";
            string part2 = $"{panelUnit} {bbSourcePart}{panelPart}".ToFixedLength(15);
            if(first.BB_NUMBER.Contains("M")) part2 = $"{panelUnit} {panelPart} {bbSourcePart}".ToFixedLength(15);
            if (first.Substation.AssetGroupName == "HV Switching Assembly")
            {
                panelUnit = "W";
                part2 = $"{panelUnit}{panelPart}".ToFixedLength(15);
            }
            string part3 = $"{assetTypeAbbreviation} ".ToFixedLength(8);
            return $"{substationSource}{part2}{part3}";
        }
        //???????remove?

        public static string GetADMSNameForCBToTransformer(SS_TO_SS_Model first, SS_TO_SS_Model second)
        {
            // ADMS Name for CB to Transformer
            string substationSource = ReplaceMultipleSpaces(first.SSNAME.Replace("S/S", "")).ToFixedLength(26);
            string bbSourcePart = string.IsNullOrEmpty(first.BB_NUMBER) ? "" : $"BD {first.BB_NUMBER}-";
            string txPart = "";
            if (first.SSNAME != second.SSNAME) // If not in the same substation
            {
                txPart = $"{ReplaceMultipleSpaces(second.SSNAME.Replace("S/S", ""))} Tx ";
                if (!string.IsNullOrEmpty(second.TX_NO)) txPart += $"D{second.TX_NO}";
            }
            else
            {
                txPart = "L/Tx ";
                if (!string.IsNullOrEmpty(second.TX_NO)) txPart += $"D{second.TX_NO}";
            }
            string part2 = ReplaceMultipleSpaces($"{bbSourcePart}{txPart}").ToFixedLength(41);
            string assetTypeAbbreviation = AssetTypeAbbreviations.ContainsKey(first.Source.AssetTypeName)
                                            ? AssetTypeAbbreviations[first.Source.AssetTypeName]
                                            : "";
            //CB LBS
            string part3 = $"{assetTypeAbbreviation}".ToFixedLength(13);

            return $"{substationSource}{part2}{part3}";
        }

        public static string GetADMSNameForTransformer(SS_TO_SS_Model first)
        {
            // Part 1: substation_name(source) (???S/S ???? "") + ??
            string substationSource = ReplaceMultipleSpaces(first.SSNAME.Replace("S/S", "")).ToFixedLength(26);

            // Part 2: "Tx " + transformer_number != null ? "D{transformer_number}" : "" + ??
            string transformerPart = string.IsNullOrEmpty(first.TX_NO) ? "" : $"D{first.TX_NO}";
            string part2 = $"Tx {transformerPart}".ToFixedLength(41);

            // Part 3: "LOAD" + ??
            string part3 = "LOAD".ToFixedLength(13);

            return $"{substationSource}{part2}{part3}";
        }

        public static string GetADMSAliasForTransformer(SS_TO_SS_Model first)
        {
            // ADMS Alias for Transformer
            string substationSource = first.SSCODE.ToFixedLength(7);
            string transformerPart = string.IsNullOrEmpty(first.TX_NO) ? "D1" : $"D{first.TX_NO}";
            //if (!string.IsNullOrEmpty(first.TX_NO)
            //    && int.TryParse(first.TX_NO, out int tx_integer)
            //    && int.Parse(first.TX_NO) >= 10)
            //    transformerPart = $"D{(char)(int.Parse(first.TX_NO) + 55)}";
            string part2 = $"{transformerPart}".ToFixedLength(15);
            string part3 = "LOAD".ToFixedLength(8);
            return $"{substationSource}{part2}{part3}";
        }
    
        public static string GetADMSNameForIsolator(Pole_Model first)
        {
            string part1, part2, part3;
            if (first.IsTxOrPMSInPole)
                part1 = ReplaceMultipleSpaces($"{first.FROM_SS_NAME?.Replace("S/S", "")}").ToFixedLength(26);
            else
                part1 = ReplaceMultipleSpaces($"{first.CIRCUIT_NAME}").ToFixedLength(26);
            part2 = $"P{first.FROM_POLE_NUM}";
            if (!string.IsNullOrEmpty(first.TO_POLE_NUM))
            {
                if (ReplaceMultipleSpaces(first.CIRCUIT_NAME) != ReplaceMultipleSpaces(first.TO_CIRCUIT_NAME))
                    part2 += $"-{first.TO_CIRCUIT_NAME} P{first.TO_POLE_NUM}";
                else part2 += $"-{first.TO_POLE_NUM}";
            }
            else if (!first.IsSingleDevice) part2 += $"-{first.TO_SS_NAME?.Replace("S/S", "")}";
            part2 = ReplaceMultipleSpaces(part2).ToFixedLength(41);
            part3 = $"ISOL".ToFixedLength(13);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSAliasForIsolator(Pole_Model first)
        {
            string part1, part2, part3;
            if (first.IsTxOrPMSInPole)
                part1 = ReplaceMultipleSpaces($"{first.FROM_SS_NUM}").ToFixedLength(7);
            else
                part1 = ReplaceMultipleSpaces($"L{first.CIRCUIT_ID}").ToFixedLength(7);
            part2 = $"P{first.FROM_POLE_NUM}";
            if (!string.IsNullOrEmpty(first.TO_POLE_NUM)) 
            {
                var splitPoleNum = first.TO_POLE_NUM?.Split("/");
                if (splitPoleNum.Length >= 2 && first.FROM_POLE_NUM == splitPoleNum[0]) 
                    part2 += $"-{splitPoleNum[1]}";
                else if (ReplaceMultipleSpaces(first.CIRCUIT_NAME) != ReplaceMultipleSpaces(first.TO_CIRCUIT_NAME))
                    part2 += $"-L{first.TO_CIRCUIT_ID} P{first.TO_POLE_NUM}";
                else part2 += $"-{first.TO_POLE_NUM}";
            }
            else if (!first.IsSingleDevice) part2 += $"-{first.TO_SS_NUM}";
            part2 = ReplaceMultipleSpaces(part2).ToFixedLength(15);
            part3 = $"ISOL".ToFixedLength(8);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSNameForFuse(Pole_Model first)
        {
            string part1, part2, part3;
            if (first.IsTxOrPMSInPole)
                part1 = ReplaceMultipleSpaces($"{first.FROM_SS_NAME?.Replace("S/S", "")}").ToFixedLength(26);
            else
                part1 = ReplaceMultipleSpaces($"{first.CIRCUIT_NAME}").ToFixedLength(26);
            part2 = $"P{first.FROM_POLE_NUM}";
            if (first.IsTxOrPMSInPole && first.InPoleType != "PMS") 
                part2 += $"-{first.FROM_SS_NAME?.Replace("S/S", "")} P/M Tx";
            else if (!first.IsSingleDevice) 
                part2 += $"-P{first.TO_POLE_NUM}";
            part2 = ReplaceMultipleSpaces(part2).ToFixedLength(41);
            part3 = $"FUSE".ToFixedLength(13);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSAliasForFuse(Pole_Model first)
        {
            string part1, part2, part3;
            part1 = ReplaceMultipleSpaces($"L{first.CIRCUIT_ID}").ToFixedLength(7);
            if (first.IsTxOrPMSInPole)
                part1 = ReplaceMultipleSpaces($"{first.FROM_SS_NUM}").ToFixedLength(7);
            part2 = $"P{first.FROM_POLE_NUM}";
            if (first.IsTxOrPMSInPole && first.InPoleType != "PMS") part2 += $"-{first.FROM_SS_NUM} P/M";
            else
            {
                if($"-P{first.TO_POLE_NUM}".Contains(part2)) 
                    part2 += $"-P{first.TO_POLE_NUM}".Replace(part2, "");
                else if (!first.IsSingleDevice) 
                    part2 += $"-P{first.TO_POLE_NUM}";
            }
            if (part2.Length >= 15) part2 = ReplaceMultipleSpaces(part2)[..14];
            part2 = part2.ToFixedLength(15);
            part3 = $"FUSE".ToFixedLength(8);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSNameForTransformer(Pole_Model first)
        {
            string part1, part2, part3;
            part1 = ReplaceMultipleSpaces($"{first.FROM_SS_NAME?.Replace("S/S", "")}").ToFixedLength(26);
            part2 = $"P/M Tx".ToFixedLength(41);
            part3 = $"LOAD".ToFixedLength(13);
            return $"{part1}{part2}{part3}";
        }

        public static string GetSOMSSForFuse(Pole_Model first)
        {
            if (first.InPoleType == "PMS" && first.IsTxOrPMSInPole) return ReplaceMultipleSpaces(first.FROM_SS_NAME?.Replace("S/S", "")) + " " + $"P.{first.FROM_POLE_NUM}";
            return ReplaceMultipleSpaces(first.CIRCUIT_NAME) + " " + $"P.{first.FROM_POLE_NUM}";
        }

        public static string GetADMSAliasForTransformer(Pole_Model first)
        {
            string part1, part2, part3;
            part1 = ReplaceMultipleSpaces($"{first.FROM_SS_NUM}").ToFixedLength(7);
            part2 = $"D0".ToFixedLength(15);
            part3 = $"LOAD".ToFixedLength(8);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSNameForPMS(Pole_Model first)
        {
            string part1, part2, part3;
            part1 = ReplaceMultipleSpaces($"{first.FROM_SS_NAME?.Replace("S/S", "")}").ToFixedLength(26);
            part2 = $"FEEDER I".ToFixedLength(41);
            part3 = $"PMS".ToFixedLength(13);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSAliasForPMS(Pole_Model first)
        {
            string part1, part2, part3;
            part1 = ReplaceMultipleSpaces($"{first.FROM_SS_NUM}").ToFixedLength(7);
            part2 = $"FDR I".ToFixedLength(15);
            part3 = $"PMS".ToFixedLength(8);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSNameForSubringCB(Pole_Model first)
        {
            // ADMS Name for Subring CB
            string substationSource = ReplaceMultipleSpaces(first.FROM_SS_NAME.Replace("S/S", "")).ToFixedLength(26);
            //string bbSourcePart = string.IsNullOrEmpty(first.BB_NUMBER) ? "" : $"BD {first.BB_NUMBER}-";
            string substationTarget = ReplaceMultipleSpaces($"{first.CIRCUIT_NAME} P{first.TO_POLE_NUM}");
            string bbTargetPart = "";

            //string serialNumberPart = string.IsNullOrEmpty(first.SERIALNUMBER) ? "" : $" #{first.SERIALNUMBER}";
            string part2 = $"{substationTarget}{bbTargetPart}".ToFixedLength(41);
            string assetTypeAbbreviation = "CB";
            string part3 = $"{assetTypeAbbreviation} ".ToFixedLength(13);
            return $"{substationSource}{part2}{part3}";
        }

        public static string GetADMSAliasForSubringCB(Pole_Model first)
        {
            // ADMS Alias for SubringCB
            string substationSource = first.FROM_SS_NUM.ToFixedLength(7);
            //string bbSourcePart = string.IsNullOrEmpty(first.BB_NUMBER) ? "" : $"B{first.BB_NUMBER}/";
            string panelPart = (string.IsNullOrEmpty(first.FROM_POLE_NUM) ? "" : first.FROM_POLE_NUM);
            string assetTypeAbbreviation = "CB";
            string panelUnit = first.FROM_SS_NAME.Contains("CUST EQPT") ? "BAY" : "PNL";
            string part2 = $"{panelUnit} {panelPart}".ToFixedLength(15);
            string part3 = $"{assetTypeAbbreviation} ".ToFixedLength(8);

            return $"{substationSource}{part2}{part3}";
        }


        public static string GetADMSNameForRecloser(Pole_Model first)
        {
            string part1, part2, part3;
            part1 = ReplaceMultipleSpaces($"{first.FROM_SS_NAME?.Replace("S/S", "")}").ToFixedLength(26);
            part2 = $"FEEDER I".ToFixedLength(41);
            if (first.FROM_SS_NAME.Contains("FRC"))
                part3 = $"FRC".ToFixedLength(13);
            else part3 = $"RC".ToFixedLength(13);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSAliasForRecloser(Pole_Model first)
        {
            string part1, part2, part3;
            part1 = ReplaceMultipleSpaces($"{first.FROM_SS_NUM}").ToFixedLength(7);
            part2 = $"FDR I".ToFixedLength(15);
            if (first.FROM_SS_NAME.Contains("FRC"))
                part3 = $"FRC".ToFixedLength(8);
            else part3 = $"RC".ToFixedLength(8);
            return $"{part1}{part2}{part3}";
        }
        public static string GetADMSNameForPoleCable(PoleCableInfo first, string objectID)
        {
            // 1. Subring CB to Pole, Pole to Pole (same circuti), Pole to Pole (different circuit)
            string part1, part2, part3;
            //part1 = $"{ReplaceMultipleSpaces(first.CIRCUIT_NAME)}";
            //part2 = $"P.{first.FROM_POLE_NUM}-P.{first.TO_POLE_NUM}";
            //if (!string.IsNullOrEmpty(first.FROM_POLE_NUM) && !string.IsNullOrEmpty(first.TO_POLE_NUM) &&
            //    CompareHierarchy(first.FROM_POLE_NUM, first.TO_POLE_NUM) > 0)
            //    part2 = $"P.{first.TO_POLE_NUM}-P.{first.FROM_POLE_NUM}";
            //// handle case Subring Circuit Breaker to Pole
            //if (first.ASSET_TYPE == "Subring Circuit Breaker")
            //    part1 = $"{ReplaceMultipleSpaces(first.FROM_SS_NAME?.Replace("S/S", ""))}";
            //else if (first.ASSET_TYPE == "Isolator" &&
            //    !string.IsNullOrEmpty(first.TO_SS_NUM))
            //{
            //    part1 = $"{ReplaceMultipleSpaces(first.TO_SS_NAME?.Replace("S/S", ""))}";
            //    part2 = $"-{ReplaceMultipleSpaces(first.CIRCUIT_NAME)} P.{first.FROM_POLE_NUM}";
            //}
            //// handle case Pole to Pole (different circuit)
            //else if (!string.IsNullOrEmpty(first.TO_CIRCUIT_ID) &&
            //   first.CIRCUIT_ID != first.TO_CIRCUIT_ID)
            //{
            //    if (first.CIRCUIT_NAME.CompareTo(first.TO_CIRCUIT_NAME) > 0)
            //    {
            //        part1 = $"{ReplaceMultipleSpaces(first.TO_CIRCUIT_NAME)}";
            //        part2 = $"P.{first.TO_POLE_NUM}-{ReplaceMultipleSpaces(first.CIRCUIT_NAME)} P.{first.FROM_POLE_NUM}";
            //    }
            //    else part2 = $"P.{first.FROM_POLE_NUM}-{first.TO_CIRCUIT_NAME} P.{first.TO_POLE_NUM}";
            //}
            part1 = $"{ReplaceMultipleSpaces(first.FromCircuit.CircuitName)}";
            part2 = $"P.{first.FromPoleNum}-{first.ToPoleNum}";
            if(!string.IsNullOrEmpty(first.FromPoleNum) && !string.IsNullOrEmpty(first.ToPoleNum) &&
                CompareHierarchy(first.FromPoleNum, first.ToPoleNum) > 0)
                part2 = $"P.{first.ToPoleNum}-{first.FromPoleNum}";
            if (!string.IsNullOrEmpty(first.Substation?.SSNum))
            {
                part1 = $"{ReplaceMultipleSpaces(first.Substation.SSName?.Replace("S/S",""))}";
                part2 = $"-{ReplaceMultipleSpaces(first.FromCircuit.CircuitName)}";
            }
            else if (!string.IsNullOrEmpty(first.ToCircuit.CircuitId) && first.FromCircuit.CircuitId != first.ToCircuit.CircuitId)
            {
                if(first.FromCircuit.CircuitName.CompareTo(first.ToCircuit.CircuitName) > 0)
                {
                    part1 = $"{ReplaceMultipleSpaces(first.ToCircuit.CircuitName)}";
                    part2 = $"P.{first.ToPoleNum}-{ReplaceMultipleSpaces(first.FromCircuit.CircuitName)} P.{first.FromPoleNum}";
                }
                else part2 = $"P.{first.FromPoleNum}-{ReplaceMultipleSpaces(first.ToCircuit.CircuitName)} P.{first.ToPoleNum}";
            }
            part3 = $"LINE_{objectID}";
            return $"{part1.ToFixedLength(24)}{part2.ToFixedLength(44)}{part3.ToFixedLength(12)}";
        }

        public static string GetADMSAliasForPoleCable(PoleCableInfo first, string objectID)
        {
            // 1. Subring CB to Pole, Pole to Pole (same circuti), Pole to Pole (different circuit)
            string part1, part2, part3;
            //part1 = $"L{ReplaceMultipleSpaces(first.CIRCUIT_ID)}";
            //part2 = $"P{first.FROM_POLE_NUM}-{first.TO_POLE_NUM}";
            //if (!string.IsNullOrEmpty(first.FROM_POLE_NUM) && !string.IsNullOrEmpty(first.TO_POLE_NUM) &&
            //    CompareHierarchy(first.FROM_POLE_NUM, first.TO_POLE_NUM) > 0)
            //    part2 = $"P{first.TO_POLE_NUM}-{first.FROM_POLE_NUM}";
            //// handle case Subring Circuit Breaker to Pole
            //if (first.ASSET_TYPE == "Subring Circuit Breaker")
            //    part1 = $"{ReplaceMultipleSpaces(first.FROM_SS_NUM)}";
            //else if (first.ASSET_TYPE == "Isolator" &&
            //    !string.IsNullOrEmpty(first.TO_SS_NUM))
            //{
            //    part1 = $"{ReplaceMultipleSpaces(first.TO_SS_NUM)}";
            //    part2 = $"-L{ReplaceMultipleSpaces(first.CIRCUIT_ID)} P{first.FROM_POLE_NUM}";
            //}
            //// handle case Pole to Pole (different circuit)
            //else if (!string.IsNullOrEmpty(first.TO_CIRCUIT_ID) &&
            //   first.CIRCUIT_ID != first.TO_CIRCUIT_ID)
            //{
            //    if (first.CIRCUIT_NAME.CompareTo(first.TO_CIRCUIT_NAME) > 0)
            //    {
            //        part1 = $"{ReplaceMultipleSpaces(first.TO_CIRCUIT_ID)}";
            //        part2 = $"P{first.TO_POLE_NUM}-L{ReplaceMultipleSpaces(first.CIRCUIT_ID)} P{first.FROM_POLE_NUM}";
            //    }
            //    else part2 = $"P{first.FROM_POLE_NUM}-L{first.TO_CIRCUIT_ID} P{first.TO_POLE_NUM}";
            //}
            part1 = $"L{ReplaceMultipleSpaces(first.FromCircuit.CircuitId)}";
            part2 = $"P{first.FromPoleNum}-{first.ToPoleNum}";
            if (!string.IsNullOrEmpty(first.FromPoleNum) && !string.IsNullOrEmpty(first.ToPoleNum) &&
                CompareHierarchy(first.FromPoleNum, first.ToPoleNum) > 0)
                part2 = $"P{first.ToPoleNum}-{first.FromPoleNum}";
            if (!string.IsNullOrEmpty(first.Substation?.SSNum))
            {
                part1 = $"{ReplaceMultipleSpaces(first.Substation.SSNum)}";
                part2 = $"-L{ReplaceMultipleSpaces(first.FromCircuit.CircuitId)}";
            }
            else if (!string.IsNullOrEmpty(first.ToCircuit.CircuitId) && first.FromCircuit.CircuitId != first.ToCircuit.CircuitId)
            {
                if (first.FromCircuit.CircuitName.CompareTo(first.ToCircuit.CircuitName) > 0)
                {
                    part1 = $"L{ReplaceMultipleSpaces(first.ToCircuit.CircuitId)}";
                    part2 = $"P{first.ToPoleNum}-L{ReplaceMultipleSpaces(first.FromCircuit.CircuitId)} P{first.FromPoleNum}";
                }
                else part2 = $"P{first.FromPoleNum}-L{ReplaceMultipleSpaces(first.ToCircuit.CircuitId)} P{first.ToPoleNum}";
            }
            part3 = $"L{objectID}";
            return $"{(part1 + " " + part2).ToFixedLength(22)}{part3.ToFixedLength(8)}";
        }

        public static string GetADMSNameForSourceFuse(LVFeature_Model first)
        {
            string part1 = ($"[{first.SUBNETWORKNAME}] {ReplaceMultipleSpaces(first.SS_NAME?.Replace("S/S", ""))}").ToFixedLength(67);
            string part2 = ($"D{first.TX_NO} CCT {first.CCT_NO}").ToFixedLength(45);
            string part3 = "LVFUSE".ToFixedLength(10);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSAliasForSourceFuse(LVFeature_Model first)
        {
            string part1 = ($"{first.SS_NUM}-{first.TX_NO}").ToFixedLength(15);
            string part2 = ($"CCT {first.CCT_NO}").ToFixedLength(15);
            string part3 = "LVFUSE".ToFixedLength(10);
            return $"{part1}{part2}{part3}";
        }

        public static string GetSOMSSForSourceFuse(LVFeature_Model first)
        {
            string transformerPart = string.IsNullOrEmpty(first.TX_NO) ? "" : $" D{first.TX_NO}";
            return $"{ReplaceMultipleSpaces(first.SS_NAME)}-L/Tx{transformerPart}-LVB";
        }

        public static string GetSOMCCTForSourceFuse(LVFeature_Model first)
        {
            return $"CCT {first.CCT_NO}";
        }

        public static string GetADMSNameForLocalSupply(LVFeature_Model first)
        {
            string part1 = ($"[{first.SUBNETWORKNAME}] {first.SPSID} {ReplaceMultipleSpaces(first.ADDRESS)}").ToFixedLength(101);
            string part2 = "LocSP".ToFixedLength(13);
            return $"{part1}{part2}";
        }

        public static string GetADMSAliasForLocalSupply(LVFeature_Model first)
        {
            string part1 = first.SPSID?.ToFixedLength(15);
            string part2 = ($"{first.SS_NUM}-{first.TX_NO}").ToFixedLength(15);
            string part3 = "LocSP".ToFixedLength(10);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSNameForSupplyPoint(LVFeature_Model first)
        {
            string subnetworkname = first.SUBNETWORKNAME;
            if (!string.IsNullOrEmpty(first.SUBNETWORKNAME))
            {
                var splitSubnetworkName = subnetworkname.Split('-');
                if (splitSubnetworkName.Length >= 2) subnetworkname = splitSubnetworkName[0];
            }
            string part1 = ($"[{subnetworkname}] {first.SPSID} {first.ADDRESS}").ToFixedLength(99);
            string part2 = "LVSP".ToFixedLength(10);
            return $"{part1}{part2}";
        }

        public static string GetADMSAliasForSupplyPoint(LVFeature_Model first)
        {
            string part1 = first.SPSID?.ToFixedLength(15);
            string part2 = ($"{first.SUBNETWORKNAME}").ToFixedLength(15);
            string part3 = "LVSP".ToFixedLength(10);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSNameForPillar(LVFeature_Model first)
        {
            string part1 = ($"{ReplaceMultipleSpaces(first.PR_NAME?.Replace("Pillar", ""))} PILLAR").ToFixedLength(90);
            string part2 = "LVPILLAR".ToFixedLength(10);
            return $"{part1}{part2}";
        }

        public static string GetADMSAliasForPillar(LVFeature_Model first)
        {
            string part1 = ($"{first.PR_NO?.PadLeft(5, '0')} PR").ToFixedLength(30);
            string part2 = "LVPILLAR".ToFixedLength(10);
            return $"{part1}{part2}";
        }

        public static string GetADMSNameForPillarFuse(LVFeature_Model first)
        {
            string part1 = ($"[{first.SUBNETWORKNAME}] {ReplaceMultipleSpaces(first.PR_NAME?.Replace("Pillar", ""))} Pillar").ToFixedLength(60);
            string part2 = ($"CCT {first.CCT_NO}").ToFixedLength(45);
            string part3 = "LVFUSE".ToFixedLength(10);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSAliasForPillarFuse(LVFeature_Model first)
        {
            string part1 = ($"{first.PR_NO?.PadLeft(5, '0')} PR").ToFixedLength(15);
            string part2 = ($"CCT {first.CCT_NO}").ToFixedLength(15);
            string part3 = "LVFUSE".ToFixedLength(10);
            return $"{part1}{part2}{part3}";
        }

        public static string GetSOMSSForPillarFuse(LVFeature_Model first)
        {
            return $"{ReplaceMultipleSpaces(first.PR_NAME)} Pillar";
        }

        public static string GetSOMCCTForPillarFuse(LVFeature_Model first)
        {
            return $"CCT {first.CCT_NO}";
        }

        public static string GetADMSNameForPoleSourceFuse(LVFeature_Model first)
        {
            string part1 = ($"[{first.SUBNETWORKNAME}] {ReplaceMultipleSpaces(first.SS_NAME)}").ToFixedLength(67);
            string part2 = ($"D{first.TX_NO} CCT {first.CCT_NO}").ToFixedLength(45);
            string part3 = "LVFUSE".ToFixedLength(10);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSAliasForPoleSourceFuse(LVFeature_Model first)
        {
            string part1 = ($"{first.SS_NUM}-{first.TX_NO}").ToFixedLength(15);
            string part2 = ($"CCT {first.CCT_NO}").ToFixedLength(15);
            string part3 = "LVFUSE".ToFixedLength(10);
            return $"{part1}{part2}{part3}";
        }

        public static string GetSOMSSForPoleSourceFuse(LVFeature_Model first)
        {
            return $"{ReplaceMultipleSpaces(first.SS_NAME)} P/M Tx";
        }

        public static string GetSOMCCTForPoleSourceFuse(LVFeature_Model first)
        {
            return $"CCT {first.CCT_NO}";
        }

        public static string GetADMSNameForMotherSupplyPoint(LVFeature_Model first)
        {
            string part1 = ($"[{first.SUBNETWORKNAME}] {ReplaceMultipleSpaces(first.SS_NAME)}").ToFixedLength(57);
            string part2 = ($"D{first.TX_NO} CCT {first.CCT_NO} P.{first.POLENUM}").ToFixedLength(45);
            string part3 = "LVFUSE".ToFixedLength(10);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSAliasForMotherSupplyPoint(LVFeature_Model first)
        {
            string part1 = ($"{first.SS_NUM}-{first.TX_NO}{first.CCT_NO}-{first.POLENUM}").ToFixedLength(15);
            string part2 = ($"P.{first.POLENUM}").ToFixedLength(15);
            string part3 = "LVFUSE".ToFixedLength(10);
            return $"{part1}{part2}{part3}";
        }

        public static string GetSOMSSForMotherSupplyPoint(LVFeature_Model first)
        {
            return ReplaceMultipleSpaces(first.ADDRESS);
        }

        public static string GetSOMCCTForMotherSupplyPoint(LVFeature_Model first)
        {
            return $"({first.SPSID})";
        }

        public static string GetADMSNameForLinkBox(LVFeature_Model first)
        {
            string part1 = ($"{first.SPSID} {ReplaceMultipleSpaces(first.ADDRESS)}").ToFixedLength(90);
            string part2 = "LVSP".ToFixedLength(10);
            return $"{part1}{part2}";
        }

        public static string GetADMSAliasForLinkBox(LVFeature_Model first)
        {
            string part1 = first.SPSID?.ToFixedLength(15);
            string part2 = first.SUBNETWORKNAME?.ToFixedLength(15);
            string part3 = "LVSP".ToFixedLength(10);
            return $"{part1}{part2}{part3}";
        }

        public static string GetADMSNameForLinkBoxLeg(LVFeature_Model first)
        {
            string part1 = ($"[{first.SUBNETWORKNAME}] {first.SPSID} {ReplaceMultipleSpaces(first.ADDRESS)}").ToFixedLength(96);
            string part2 = ($"LEG {first.LEG} LVLB LEG").ToFixedLength(16);
            return $"{part1}{part2}";
        }

        public static string GetADMSAliasForLinkBoxLeg(LVFeature_Model first)
        {
            string part1 = first.SPSID?.ToFixedLength(15);
            string part2 = ($"LEG {first.LEG}").ToFixedLength(15);
            string part3 = "LVLB".ToFixedLength(10);
            return $"{part1}{part2}{part3}";
        }

        public static string GetSOMSSForLinkBoxLeg(LVFeature_Model first)
        {
            return ReplaceMultipleSpaces(first.ADDRESS);
        }

        public static string GetSOMCCTForLinkBoxLeg(LVFeature_Model first)
        {
            return $"LEG {first.LEG}({first.SPSID})";
        }
    }

}
