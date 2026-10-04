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
}

[ByRefEvent]
public readonly struct StripHandInsertStartedEvent(EntityUid recipient, EntityUid item)
{
    public readonly EntityUid Recipient = recipient;
    public readonly EntityUid Item = item;
}

[ByRefEvent]
public struct BeforeStripHandRemoveEvent(EntityUid user, EntityUid holder, string handName)
{
    public readonly EntityUid User = user;
    public readonly EntityUid Holder = holder;
    public readonly string HandName = handName;
    public bool Handled;
}
