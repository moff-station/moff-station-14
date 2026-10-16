using Robust.Shared.GameStates;

namespace Content.Shared._Starfall.Offering;

/// <summary>
/// Marks a held item as being offered by its holder to a specific recipient.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(OfferingSystem))]
public sealed partial class OfferedItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Offerer;

    [DataField, AutoNetworkedField]
    public EntityUid Recipient;

    /// <summary>
    /// Id of the offerer's hand-insert doafter; accepting completes it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ushort DoAfterIndex;
}

[ByRefEvent]
public record struct StripHandInsertStartedEvent(EntityUid Recipient, EntityUid Item, ushort DoAfterId);

[ByRefEvent]
public record struct BeforeStripHandRemoveEvent(EntityUid User, EntityUid Holder)
{
    public readonly EntityUid User = User;
    public readonly EntityUid Holder = Holder;
    public bool Handled;
}
