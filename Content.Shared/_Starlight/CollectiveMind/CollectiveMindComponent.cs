using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.CollectiveMind
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class CollectiveMindComponent : Component
    {
        [DataField]
        public Dictionary<CollectiveMindPrototype, CollectiveMindMemberData> Minds = new();
    }

    /// <summary>
    /// Stores data about the collective mind member.
    /// </summary>
    [Serializable, DataDefinition]
    public sealed partial class CollectiveMindMemberData
    {
        [DataField(required: true)]
        public int MindId; //this value determines the starting mind id for members of the collective mind.

        public const int StartingId = 1;
    }
}
