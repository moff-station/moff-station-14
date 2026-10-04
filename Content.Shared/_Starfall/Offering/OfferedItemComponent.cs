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
    /// Index of the offerer's hand-insert doafter; accepting completes it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ushort DoAfterIndex;
}

[ByRefEvent]
public readonly struct StripHandInsertStartedEvent(EntityUid recipient, EntityUid item, ushort doAfterIndex)
{
    public readonly EntityUid Recipient = recipient;
    public readonly EntityUid Item = item;
    public readonly ushort DoAfterIndex = doAfterIndex;
}

[ByRefEvent]
public struct BeforeStripHandRemoveEvent(EntityUid user, EntityUid holder)
{
    public readonly EntityUid User = user;
    public readonly EntityUid Holder = holder;
    public bool Handled;
}
