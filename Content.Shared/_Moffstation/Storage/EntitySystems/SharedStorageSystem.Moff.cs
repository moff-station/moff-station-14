using Content.Shared.Hands.Components;

// ReSharper disable once CheckNamespace // Moff addition to upstream file
namespace Content.Shared.Storage.EntitySystems;

public abstract partial class SharedStorageSystem
{
    /// <summary>
    /// Places an item stored in this storage into a player's active hand.
    /// </summary>
    public bool GrabItem(Entity<StorageComponent?> storage, Entity<HandsComponent?> player, EntityUid item)
    {
        if (!Resolve(storage, ref storage.Comp, false)
            || !storage.Comp.Container.Contains(item)
            || !_sharedHandsSystem.TryPickup(player, item, handsComp: player.Comp))
            return false;

        if (storage.Comp.StorageRemoveSound != null
            && !_tag.HasTag(player, storage.Comp.SilentStorageUserTag))
        {
            Audio.PlayPredicted(storage.Comp.StorageRemoveSound, storage, player, _audioParams);
        }

        return true;
    }
}
