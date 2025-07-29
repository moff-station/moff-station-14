namespace Content.Server.DeviceNetwork.Components
{
    [RegisterComponent]
    [ComponentProtoName("WiredNetworkConnection")]
    public sealed partial class WiredNetworkComponent : Component
    {
        /// <summary>
        /// If true, will bypass the same-grid check that wired networks usually have
        /// </summary>
        [DataField]
        public bool LongRange;
    }
}
