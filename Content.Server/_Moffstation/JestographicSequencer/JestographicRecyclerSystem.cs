using Content.Shared._Moffstation.JestographicSequencer;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Materials;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.JestographicSequencer;

/// <summary>
/// Dresses mobs that fall into a recycler jested by a jestographic sequencer.
/// </summary>
public sealed partial class JestographicRecyclerSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMaterialReclaimerSystem _reclaimer = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JestographicRecyclerComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<JestographicRecyclerComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<CollideMaterialReclaimerComponent>(ent, out var collide)
            || args.OurFixtureId != collide.FixtureId
            || !TryComp<MaterialReclaimerComponent>(ent, out var reclaimer)
            || !_reclaimer.CanStart(ent, reclaimer))
            return;

        var victim = args.OtherEntity;
        if (!HasComp<MobStateComponent>(victim) || !HasComp<InventoryComponent>(victim))
            return;

        _audio.PlayPvs(ent.Comp.Sound, victim);

        foreach (var (slot, proto) in ent.Comp.Outfit)
        {
            Dress(victim, slot, proto);
        }

        _stun.TryUpdateParalyzeDuration(victim, ent.Comp.StunDuration);
    }

    /// <summary>
    /// Equips the outfit and drops any item it replaces on the ground.
    /// </summary>
    private void Dress(EntityUid victim, string slot, EntProtoId proto)
    {
        if (!_inventory.TryGetSlotContainer(victim, slot, out var container, out _))
            return;

        if (container.ContainedEntity is { } old)
        {
            // Already wearing it from an earlier pass.
            if (Prototype(old)?.ID == proto.Id)
                return;

            // Removed directly rather than unequipped, so items in dependent slots (pockets, ID) aren't dropped with it.
            if (!_container.Remove(old, container, force: true))
                return;

            // Their old clothes are left on the floor.
            _transform.DropNextTo(old, victim);
        }

        var item = Spawn(proto, Transform(victim).Coordinates);
        if (!_inventory.TryEquip(victim, item, slot, silent: true, force: true))
            Del(item);
    }
}
