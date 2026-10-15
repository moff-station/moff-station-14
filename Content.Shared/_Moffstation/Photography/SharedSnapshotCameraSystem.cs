using System.Numerics;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Photography;

/// <summary>
/// Using the camera in hand raises it to the eye when raised you can photograph a spot you click on.
/// </summary>
/// <remarks>
/// Only the client knows where its screen ends, so it decides where the frame sits (clamped to the screen) and sends
/// that as a <see cref="SnapshotCameraShootEvent"/>. Everything else about the shot is checked here.
/// </remarks>
public abstract partial class SharedSnapshotCameraSystem : EntitySystem
{
    [Dependency] private SharedFlashSystem _flash = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedContentEyeSystem _contentEye = default!;
    [Dependency] protected SharedTransformSystem Transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SnapshotCameraComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<SnapshotCameraComponent, GotUnequippedHandEvent>(OnUnequipped);
        SubscribeLocalEvent<SnapshotCameraComponent, HandDeselectedEvent>(OnDeselected);
        SubscribeLocalEvent<SnapshotCameraComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
        SubscribeAllEvent<SnapshotCameraShootEvent>(OnShootRequest);
    }

    private void OnUseInHand(Entity<SnapshotCameraComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        SetAiming(ent, !ent.Comp.Aiming, args.User);
        _popup.PopupClient(Loc.GetString(ent.Comp.Aiming ? "photo-camera-raise" : "photo-camera-lower"), args.User, args.User);
    }

    private void OnUnequipped(Entity<SnapshotCameraComponent> ent, ref GotUnequippedHandEvent args)
    {
        SetAiming(ent, false, args.User);
    }

    /// <summary>
    /// Switching to the other hand lowers the camera, so it isn't still raised when switching back.
    /// </summary>
    private void OnDeselected(Entity<SnapshotCameraComponent> ent, ref HandDeselectedEvent args)
    {
        if (!ent.Comp.Aiming)
            return;

        SetAiming(ent, false, args.User);
        _popup.PopupClient(Loc.GetString("photo-camera-lower"), args.User, args.User);
    }

    /// <remarks>
    /// Raised for every click with the camera in hand, near or far, so handling it here keeps clicks from also
    /// interacting with whatever was clicked. The shot itself comes from the client's <see cref="SnapshotCameraShootEvent"/>.
    /// </remarks>
    private void OnBeforeInteract(Entity<SnapshotCameraComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !ent.Comp.Aiming)
            return;

        args.Handled = true;
        OnAimedClick(ent, args.User, args.ClickLocation);
    }

    /// <summary>
    /// A click while looking through the viewfinder. The client turns this into a shot request.
    /// </summary>
    protected virtual void OnAimedClick(Entity<SnapshotCameraComponent> ent, EntityUid user, EntityCoordinates click)
    {
    }

    private void OnShootRequest(SnapshotCameraShootEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user
            || !TryGetEntity(ev.Camera, out var cameraUid)
            || !TryComp<SnapshotCameraComponent>(cameraUid, out var camera)
            || !camera.Aiming
            || _hands.GetActiveItem(user) != cameraUid)
        {
            return;
        }

        // The center comes from the client, so the range is enforced here.
        var center = Transform.ToMapCoordinates(GetCoordinates(ev.Center));
        var from = Transform.GetMapCoordinates(user);
        if (center.MapId != from.MapId || (center.Position - from.Position).Length() > camera.MaxRange)
        {
            _popup.PopupClient(Loc.GetString("photo-camera-too-far"), user, user);
            return;
        }

        if (TryComp<FlashComponent>(cameraUid, out var flash))
        {
            // Uses a charge, respects the use delay and plays the shutter sound.
            if (!_flash.TryUseFlashItem((cameraUid.Value, flash), user))
                return;

            _flash.FlashArea(cameraUid.Value, user, flash.Range, flash.AoeFlashDuration, flash.SlowTo, true, flash.Probability);
        }

        var shot = new SnapshotCameraShotEvent(user, center);
        RaiseLocalEvent(cameraUid.Value, ref shot);
    }

    public void SetAiming(Entity<SnapshotCameraComponent> ent, bool aiming, EntityUid? user)
    {
        if (ent.Comp.Aiming == aiming)
            return;

        ent.Comp.Aiming = aiming;
        Dirty(ent);

        // Looking through the viewfinder extends the view, which needs a larger PVS range.
        if (user != null)
            _contentEye.UpdatePvsScale(user.Value);
    }

    /// <summary>
    /// Ensures the Snapshot is what the server shows
    /// </summary>
    public PhotoFrame GetFrame(MapCoordinates center, Vector2i halfExtents)
    {
        EntityUid? grid = null;
        var rotation = Angle.Zero;
        var frameToWorld = Matrix3x2.Identity;
        var worldToFrame = Matrix3x2.Identity;
        if (_map.TryFindGridAt(center, out var gridUid, out _))
        {
            grid = gridUid;
            (_, rotation, frameToWorld, worldToFrame) = Transform.GetWorldPositionRotationMatrixWithInv(gridUid);
        }

        var local = Vector2.Transform(center.Position, worldToFrame);
        var centerTile = new Vector2i((int) MathF.Floor(local.X), (int) MathF.Floor(local.Y));
        return new PhotoFrame(grid, centerTile, halfExtents, rotation, frameToWorld, worldToFrame);
    }
}

/// <summary>
/// Sent by the client to take a shot centered on <see cref="Center"/>, which it has kept on screen.
/// </summary>
[Serializable, NetSerializable]
public sealed class SnapshotCameraShootEvent(NetEntity camera, NetCoordinates center) : EntityEventArgs
{
    public readonly NetEntity Camera = camera;
    public readonly NetCoordinates Center = center;
}

public readonly record struct PhotoFrame(
    EntityUid? Grid,
    Vector2i CenterTile,
    Vector2i HalfExtents,
    Angle Rotation,
    Matrix3x2 FrameToWorld,
    Matrix3x2 WorldToFrame)
{
    /// <summary>
    /// The covered area in frame-local coordinates.
    /// </summary>
    public Box2 Box => new(CenterTile - HalfExtents, CenterTile + HalfExtents + Vector2i.One);
}
