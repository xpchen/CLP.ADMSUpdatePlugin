using ArcGIS.Desktop.Framework.Contracts;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CLP.ADMSUpdatePlugin
{
    public class CableCircuit
    {
        public string CircuitName { get; set; }
        public string CircuitId { get; set; }
    }

    public class CableSubstation
    {
        public string SSName { get; set; }
        public string SSNum { get; set; }
    }

    public class PoleCableInfo : PropertyChangedBase
    {
        [JsonIgnore]
        public FeatureSnapshot CableFeature { get; set; }

        public string CableObjectID { get; set; }

        public string CableAssetType { get; set; }

        public string FromPoleNum { get; set; }

        public string ToPoleNum { get; set; }

        public CableCircuit FromCircuit { get; set; }

        public CableCircuit ToCircuit { get; set; }

        public CableSubstation Substation { get; set; }

        private string _admsName;
        public string ADMSName
        {
            get => _admsName;
            set => SetProperty(ref _admsName, value);
        }

        private string _admsAlias;
        public string ADMSAlias
        {
            get => _admsAlias;
            set => SetProperty(ref _admsAlias, value);
        }
    }
}
