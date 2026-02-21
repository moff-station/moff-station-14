using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Movement.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Content.Shared.Examine;
using Content.Shared.Localizations;

namespace Content.Shared.Weapons.Ricochet;

/// <summary>
/// This handles Ricocheting projectiles and hitscan shots.
/// </summary>
public sealed class RicochetSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RicochetComponent, ProjectileReflectAttemptEvent>(OnRicochetCollide);
    }

    private void OnRicochetCollide(Entity<RicochetComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryRicochetProjectile(ent, ent.Owner, args.ProjUid, args.TargetUid))
            args.Cancelled = true;
    }

    private bool TryRicochetProjectile(Entity<RicochetComponent> ent, EntityUid user, Entity<ProjectileComponent?> projectile, EntityUid target)
    {
        if (
            !TryComp<RicochetComponent>(ent, out var ricocheting) ||
	    !_random.Prob(ricocheting.RicochetProb) ||
            !TryComp<PhysicsComponent>(projectile, out var physicsProj) ||
            !TryComp<PhysicsComponent>(target, out var physicsTarg) ||
	    HasComp<MobCollisionComponent>(target)) // No ricochet off mobs
        {
	    _adminLogger.Add(LogType.BulletHit, LogImpact.Low, $"no ricochet {ToPrettyString(ent)}");
            return false;
        }

	var (worldPosProj, worldRotProj) = _transform.GetWorldPositionRotation(user);
	var (worldPosTarg, worldRotTarg) = _transform.GetWorldPositionRotation(target);

	var bounceVec = (worldPosProj-worldPosTarg).Normalized() * physicsProj.LinearVelocity.Length();
	var rotation = new Angle(bounceVec);

        _physics.SetLinearVelocity(projectile, bounceVec, body: physicsProj);

        var locRot = Transform(projectile).LocalRotation;
        var newRot = rotation.RotateVec(locRot.ToVec());
        _transform.SetLocalRotation(projectile, newRot.ToAngle());

        PlayAudioAndPopup(ricocheting, user);

	// Prevent multiple bounce off same object
        if (Resolve(projectile, ref projectile.Comp, false))
        {
            _adminLogger.Add(LogType.BulletHit, LogImpact.Medium, $"{ToPrettyString(user)} ricocheted {ToPrettyString(projectile)} from {ToPrettyString(projectile.Comp.Weapon)} shot by {projectile.Comp.Shooter}");

            projectile.Comp.Shooter = target;
            projectile.Comp.Weapon = target;
            Dirty(projectile, projectile.Comp);
        }
        else
        {
            _adminLogger.Add(LogType.BulletHit, LogImpact.Medium, $"{ToPrettyString(user)} ricocheted {ToPrettyString(projectile)}");
        }

	// Reduce ricochet likelihood based off drop value
	ricocheting.RicochetProb = ricocheting.RicochetProb * (1.0f - ricocheting.RicochetDrop);

        return true;
    }

    private void PlayAudioAndPopup(RicochetComponent ricocheting, EntityUid user)
    {
        // Can probably be changed for prediction
        if (_netManager.IsServer)
        {
            _popup.PopupEntity(Loc.GetString("reflect-shot"), user);
            _audio.PlayPvs(ricocheting.SoundOnReflect, user);
        }
    }
}
