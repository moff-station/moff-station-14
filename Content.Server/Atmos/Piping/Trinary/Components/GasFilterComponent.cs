using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Trinary.Components
{
    [RegisterComponent]
    public sealed partial class GasFilterComponent : Component
    {
        [DataField]
        public bool Enabled = true;

        [DataField("inlet")]
        public string InletName = "inlet";

        [DataField("filter")]
        public string FilterName = "filter";

        [DataField("outlet")]
        public string OutletName = "outlet";

        [DataField]
        public float TransferRate = Atmospherics.MaxTransferRate;

        [DataField]
        public float MaxTransferRate = Atmospherics.MaxTransferRate;

        /// <remark>
        /// Moffstation - this is left here to ensure compatibility with maps
        /// starting with pre-set gas filters.
        /// </remark>
        [DataField]
        public Gas? FilteredGas;

        [DataField]
        public HashSet<Gas> FilteredGases = []; // Moffstation - filter multiple gases
    }
}
