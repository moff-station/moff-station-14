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

        var dressed = false;
        foreach (var (slot, proto) in ent.Comp.Outfit)
        {
            dressed |= Dress(victim, slot, proto);
        }

        if (!dressed)
            return;

        _stun.TryUpdateParalyzeDuration(victim, ent.Comp.StunDuration);
        _audio.PlayPvs(ent.Comp.Sound, victim);
    }

    /// <summary>
    /// Returns true if the slot was changed.
    /// </summary>
    private bool Dress(EntityUid victim, string slot, EntProtoId proto)
    {
        if (!_inventory.TryGetSlotContainer(victim, slot, out var container, out _))
            return false;

        if (container.ContainedEntity is { } old)
        {
            // Already wearing it from an earlier pass.
            if (Prototype(old)?.ID == proto.Id && HasComp<UnremoveableComponent>(old))
                return false;

            // Removed directly rather than unequipped, so items in dependent slots (pockets, ID) aren't dropped with it.
            if (!_container.Remove(old, container, force: true))
                return false;

            // Their old clothes are left on the floor.
            _transform.DropNextTo(old, victim);
        }

        var item = Spawn(proto, Transform(victim).Coordinates);
        EnsureComp<UnremoveableComponent>(item);
        if (_inventory.TryEquip(victim, item, slot, silent: true, force: true))
            return true;

        Del(item);
        return false;
    }
}
