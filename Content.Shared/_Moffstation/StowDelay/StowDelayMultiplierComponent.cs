using Content.Shared.DoAfter;
using Content.Shared.Storage;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.StowDelay;

/// <summary>
/// Scales how long it takes to move items in and out of this storage.
/// </summary>
[RegisterComponent, Access(typeof(StowDelaySystem))]
public sealed partial class StowDelayMultiplierComponent : Component
{
    [DataField]
    public float Multiplier = 1f;

    [DataField]
    public LocId ExamineText = "stow-delay-examine-multiplier";
}

[Serializable, NetSerializable]
public sealed partial class StowStorageDoAfterEvent : DoAfterEvent
{
    public ItemStorageLocation? Location;

    public StowStorageDoAfterEvent(ItemStorageLocation? location)
    {
        Location = location;
    }

    public override DoAfterEvent Clone() => this;
}

/// <summary>
/// Takes an item out of storage into the user's hand, or into another storage when TransferStorage is set.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class UnstowStorageDoAfterEvent : DoAfterEvent
{
    public NetEntity? TransferStorage;

    public ItemStorageLocation? Location;

    public UnstowStorageDoAfterEvent(NetEntity? transferStorage, ItemStorageLocation? location)
    {
        TransferStorage = transferStorage;
        Location = location;
    }

    public override DoAfterEvent Clone() => this;
}

/// <summary>
/// Moves everything in one storage into the doafter's target storage.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TransferAllStorageDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class StowEquipDoAfterEvent : DoAfterEvent
{
    public string Slot;

    public StowEquipDoAfterEvent(string slot)
    {
        Slot = slot;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class UnstowEquipDoAfterEvent : DoAfterEvent
{
    public string Slot;

    public UnstowEquipDoAfterEvent(string slot)
    {
        Slot = slot;
    }

    public override DoAfterEvent Clone() => this;
}
