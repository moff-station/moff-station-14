using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Sprite;

/// This component changes sprite layers based on whether or not the item is currently
/// <see cref="SharedHandsSystem.IsHolding(Robust.Shared.GameObjects.Entity{Content.Shared.Hands.Components.HandsComponent?},Robust.Shared.GameObjects.EntityUid?)">being held</see>.
[RegisterComponent]
public sealed partial class SpriteChangesWhenHeldComponent : Component
{
    /// Layer and sprite state to use when held.
    [DataField]
    public Dictionary<string, PrototypeLayerData> HeldLayers = new();

    /// Layer and sprite state to use when not held.
    [DataField]
    public Dictionary<string, PrototypeLayerData> NotHeldLayers = new();
}

/// This system updates <see cref="AppearanceComponent">entity appearances</see> based on
/// <see cref="SpriteChangesWhenHeldComponent"/>.
public sealed partial class SpriteChangesWhenHeldSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        // Update layers when the component is added / entity is created.
        SubscribeLocalEvent<SpriteChangesWhenHeldComponent, ComponentInit>(OnContainerChanged);

        SubscribeLocalEvent<SpriteChangesWhenHeldComponent, EntGotInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<SpriteChangesWhenHeldComponent, EntGotRemovedFromContainerMessage>(OnContainerChanged);
    }

    private void OnContainerChanged<T>(Entity<SpriteChangesWhenHeldComponent> entity, ref T args)
    {
        var appearance = EnsureComp<AppearanceComponent>(entity);
        var hasData = _appearance.TryGetData<bool>(entity, SpriteChangesWhenHeldVisuals.IsHeld, out var isHeldData, appearance);
        var isHeld = _hands.IsHolding(Transform(entity).ParentUid, entity);
        if (!hasData || // If there was no appearance data, force it to be updated.
            isHeldData != isHeld)
        {
            _appearance.SetData(entity, SpriteChangesWhenHeldVisuals.IsHeld, isHeld, appearance);
        }
    }
}

/// This enum is just a key for appearance data related to <see cref="SpriteChangesWhenHeldComponent"/>.
[Serializable, NetSerializable]
public enum SpriteChangesWhenHeldVisuals
{
    IsHeld,
}
