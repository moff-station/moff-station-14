using System.Linq;
using Content.Client._Moffstation.GameObjects;
using Content.Shared._Moffstation.BladeServer;
using Robust.Client.GameObjects;

namespace Content.Client._Moffstation.BladeServer;

/// <summary>
/// This is a <see cref="VisualizerSystem{T}"/> for entities with <see cref="BladeServerRackComponent"/>. It manages
/// dynamic sprite layers for blade servers inserted into server racks.
/// </summary>
public sealed partial class BladeServerRackVisualizerSystem : ManagedLayerVisualizerSystem<BladeServerRackComponent>
{
    protected override ref HashSet<string> GetSpriteLayersAdded(BladeServerRackComponent component) =>
        ref component.AddedLayerKeys;

    protected override void AddLayersOnAppearanceChange(
        BladeServerRackComponent component,
        Entity<SpriteComponent?> sprite,
        AppearanceComponent appearance,
        LayerFactory layerFactory
    )
    {
        if (!AppearanceSystem.TryGetData<BladeServerRackSlotVisualData.Group>(
                sprite,
                BladeServerRackVisuals.SlotsKey,
                out var slots,
                appearance
            ))
            return;

        foreach (var (idx, slotNullable) in slots.Slots.Index())
        {
            if (slotNullable is not { } slot)
                // Slot is empty.
                continue;

            // Add a base layer for the blade frame.
            layerFactory($"blade-base-{idx}",
                new PrototypeLayerData
                {
                    State = "blade-base",
                    Offset = slot.Offset,
                });

            // Add another layer for the stripe.
            if (slot.StripeColor is { } color)
            {
                layerFactory(
                    $"blade-stripe-{idx}",
                    new PrototypeLayerData
                    {
                        State = "blade-stripe",
                        Color = color,
                        Offset = slot.Offset,
                    }
                );
            }

            // Add another layer for power indicating lights.
            if (slot.Powered)
            {
                layerFactory(
                    $"blade-powered-{idx}",
                    new PrototypeLayerData
                    {
                        State = $"blade-powered-{idx}",
                        Shader = SpriteSystem.UnshadedId,
                        Offset = slot.Offset,
                    }
                );
            }
        }
    }
}
