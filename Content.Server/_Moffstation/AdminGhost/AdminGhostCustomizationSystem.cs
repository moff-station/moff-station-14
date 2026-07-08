using System.Numerics;
using System.Threading.Tasks;
using Content.Server.MapText;
using Content.Shared._Moffstation.AdminGhost;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Overlays;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.AdminGhost;

public sealed partial class AdminGhostCustomizationSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private MapTextSystem _mapText = default!;
    [Dependency] private MovementSpeedModifierSystem _moveSpeed = default!;
    [Dependency] private AdminGhostSaveManager _saveManager = default!;

    public void ApplySavedDataIfExists(EntityUid uid, NetUserId userId)
    {
        var data = _saveManager.GetData(userId);
        if (data == null)
            return;

        ApplySavedData(uid, data);
    }

    public async Task<bool> SaveCurrentToDb(EntityUid uid, NetUserId userId, AdminGhostSaveManager saveManager, AdminGhostCustomizationComponent? comp = null)
    {
        if (!TryComp(uid, out comp))
            return false;

        var data = new AdminGhostSavedData
        {
            SpritePrototype = comp.SpritePrototype,
            CustomName = comp.CustomName,
            CustomDescription = comp.CustomDescription,
            WalkSpeed = comp.WalkSpeed,
            SprintSpeed = comp.SprintSpeed,
            MapText = comp.MapText != null ? new SavedMapTextData
            {
                Text = comp.MapText.Text,
                ColorHex = comp.MapText.Color.ToHex(),
                FontSize = comp.MapText.FontSize,
                Offset = comp.MapText.Offset,
            } : null,
            ShowJobIcons = HasComp<ShowJobIconsComponent>(uid),
            ShowCriminalRecordIcons = HasComp<ShowCriminalRecordIconsComponent>(uid),
            ShowMindShieldIcons = HasComp<ShowMindShieldIconsComponent>(uid),
            ShowSyndicateIcons = HasComp<ShowSyndicateIconsComponent>(uid),
            ShowHealthBars = HasComp<ShowHealthBarsComponent>(uid),
        };

        return await saveManager.SetData(userId, data);
    }

    public void ApplySavedData(EntityUid uid, AdminGhostSavedData data)
    {
        if (data.SpritePrototype != null)
            SetSpritePrototype(uid, data.SpritePrototype);

        if (data.CustomName != null)
            SetCustomName(uid, data.CustomName);

        if (data.CustomDescription != null)
            SetCustomDescription(uid, data.CustomDescription);

        if (data.WalkSpeed.HasValue)
            SetWalkSpeed(uid, data.WalkSpeed);

        if (data.SprintSpeed.HasValue)
            SetSprintSpeed(uid, data.SprintSpeed);

        if (data.MapText != null && data.MapText.Text != null)
        {
            var color = data.MapText.ColorHex != null && Color.TryFromHex(data.MapText.ColorHex) is { } parsed
                ? parsed
                : Color.White;
            var mapTextData = new MapTextData
            {
                Text = data.MapText.Text,
                Color = color,
                FontSize = data.MapText.FontSize ?? 12,
                Offset = data.MapText.Offset ?? Vector2.Zero,
            };
            SetMapText(uid, mapTextData);
        }

        if (data.ShowJobIcons)
            ShowOverlay<ShowJobIconsComponent>(uid);
        if (data.ShowCriminalRecordIcons)
            ShowOverlay<ShowCriminalRecordIconsComponent>(uid);
        if (data.ShowMindShieldIcons)
            ShowOverlay<ShowMindShieldIconsComponent>(uid);
        if (data.ShowSyndicateIcons)
            ShowOverlay<ShowSyndicateIconsComponent>(uid);
        if (data.ShowHealthBars)
            ShowOverlay<ShowHealthBarsComponent>(uid);
    }

    public void SetSpritePrototype(EntityUid uid, EntProtoId? protoId, AdminGhostCustomizationComponent? comp = null)
    {
        comp ??= EnsureComp<AdminGhostCustomizationComponent>(uid);

        comp.SpritePrototype = protoId;
        Dirty(uid, comp);
    }

    public void SetCustomName(EntityUid uid, string? name, AdminGhostCustomizationComponent? comp = null)
    {
        comp ??= EnsureComp<AdminGhostCustomizationComponent>(uid);

        comp.CustomName = name;
        _metaData.SetEntityName(uid, name ?? "admin observer");
    }

    public void SetCustomDescription(EntityUid uid, string? desc, AdminGhostCustomizationComponent? comp = null)
    {
        comp ??= EnsureComp<AdminGhostCustomizationComponent>(uid);

        comp.CustomDescription = desc;
        _metaData.SetEntityDescription(uid, desc ?? "Boo!");
    }

    public void SetWalkSpeed(EntityUid uid, float? speed, AdminGhostCustomizationComponent? comp = null)
    {
        comp ??= EnsureComp<AdminGhostCustomizationComponent>(uid);

        comp.WalkSpeed = speed;
        _moveSpeed.ChangeBaseSpeed(uid, speed ?? 8f, TryComp<MovementSpeedModifierComponent>(uid, out var move) ? move.BaseSprintSpeed : 12f, 20f, move);
    }

    public void SetSprintSpeed(EntityUid uid, float? speed, AdminGhostCustomizationComponent? comp = null)
    {
        comp ??= EnsureComp<AdminGhostCustomizationComponent>(uid);

        comp.SprintSpeed = speed;
        _moveSpeed.ChangeBaseSpeed(uid, TryComp<MovementSpeedModifierComponent>(uid, out var move) ? move.BaseWalkSpeed : 8f, speed ?? 12f, 20f, move);
    }

    public void SetMapText(EntityUid uid, MapTextData? data, AdminGhostCustomizationComponent? comp = null)
    {
        comp ??= EnsureComp<AdminGhostCustomizationComponent>(uid);

        comp.MapText = data;

        if (data == null)
        {
            _mapText.Clear(uid);
            return;
        }

        _mapText.SetData(uid, data.Text, data.Color, data.FontSize, data.Offset);
    }

    public void ShowOverlay<T>(EntityUid uid, AdminGhostCustomizationComponent? comp = null) where T : Component, new()
    {
        comp ??= EnsureComp<AdminGhostCustomizationComponent>(uid);

        EnsureComp<T>(uid);
    }

    public void HideOverlay<T>(EntityUid uid, AdminGhostCustomizationComponent? comp = null) where T : Component, new()
    {
        comp ??= EnsureComp<AdminGhostCustomizationComponent>(uid);

        RemComp<T>(uid);
    }

    public void ResetAll(EntityUid uid, AdminGhostCustomizationComponent? comp = null)
    {
        comp ??= EnsureComp<AdminGhostCustomizationComponent>(uid);

        SetSpritePrototype(uid, null, comp);
        SetCustomName(uid, null, comp);
        SetCustomDescription(uid, null, comp);
        SetWalkSpeed(uid, null, comp);
        SetSprintSpeed(uid, null, comp);
        SetMapText(uid, null, comp);

        HideOverlay<ShowJobIconsComponent>(uid, comp);
        HideOverlay<ShowCriminalRecordIconsComponent>(uid, comp);
        HideOverlay<ShowMindShieldIconsComponent>(uid, comp);
        HideOverlay<ShowSyndicateIconsComponent>(uid, comp);
        HideOverlay<ShowHealthBarsComponent>(uid, comp);

        Dirty(uid, comp);
        RemComp<AdminGhostCustomizationComponent>(uid);
    }
}
