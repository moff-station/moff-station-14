using Content.Shared._Moffstation.Extensions;
using Content.Shared._Moffstation.Trigger.Components.Effects;
using Content.Shared.Trigger;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Trigger.Systems;

/// This system implements the behavior of <see cref="SetAppearanceOnTriggerComponent"/>.
public sealed partial class SetAppearanceOnTriggerSystem : XOnTriggerSystem<SetAppearanceOnTriggerComponent>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    [Dependency] private readonly EntityQuery<AppearanceComponent> _appearanceQuery = default!;

    [Dependency]
    private readonly EntityQuery<SetAppearanceOnTriggerRestorationComponent> _setAppearanceOnTriggerRestorationQuery =
        default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SetAppearanceOnTriggerComponent, MapInitEvent>(OnMapInit);
    }

    protected override void OnTrigger(
        Entity<SetAppearanceOnTriggerComponent> ent,
        EntityUid target,
        ref TriggerEvent args
    )
    {
        if (GetAppearance(ent) is not { } appearance)
            return;

        // If we have a restore time already, just update the restore time based on this trigger invocation.
        var alreadySet = _setAppearanceOnTriggerRestorationQuery.TryComp(ent, out var restoreComp);

        if (ent.Comp.Duration is { } duration)
        {
            restoreComp ??= EnsureComp<SetAppearanceOnTriggerRestorationComponent>(ent);
            restoreComp.RestoreAt = _timing.CurTime + duration;
        }

        // We're already set. Trying to set the restore data at this point would override legitimate restore data with
        // our set data, so just return now.
        if (alreadySet)
            return;

        restoreComp?.RestoreData.Clear();
        foreach (var (key, data) in ent.Comp.Data)
        {
            if (restoreComp != null)
            {
                _appearance.TryGetData(ent, key, out var restoreData, appearance);
                restoreComp.RestoreData.Add((key, restoreData));
            }

            _appearance.SetData(ent, key, data, appearance);
        }
    }

    private void OnMapInit(Entity<SetAppearanceOnTriggerComponent> ent, ref MapInitEvent args)
    {
        // Helpful debug assert to tell you that you need AppearanceComponent for this component to work.
        DebugTools.Assert(
            _appearanceQuery.HasComp(ent),
            $"{ToPrettyString(ent)} has {nameof(SetAppearanceOnTriggerComponent)} but no {nameof(AppearanceComponent)}. {nameof(SetAppearanceOnTriggerComponent)} doesn't do anything without {nameof(AppearanceComponent)} on the same entity."
        );
    }

    /// Handles restoring appearance data.
    public override void Update(float frameTime)
    {
        var q = AllEntityQuery<SetAppearanceOnTriggerRestorationComponent>();
        while (q.MoveNext(out var ent, out var comp))
        {
            if (comp.RestoreAt > _timing.CurTime ||
                GetAppearance(ent) is not { } appearance)
                continue;

            foreach (var (key, data) in comp.RestoreData)
            {
                _appearance.SetOrRemoveData(appearance, key, data);
            }

            RemCompDeferred(ent, comp);
        }
    }

    private Entity<AppearanceComponent>? GetAppearance(EntityUid ent)
    {
        AppearanceComponent? comp = null;
        return _appearanceQuery.Resolve(ent, ref comp) ? (ent, comp) : null;
    }
}

[Serializable, NetSerializable]
public enum SetAppearanceOnTriggerVisuals : byte
{
    [UsedImplicitly]
    Key,
}
