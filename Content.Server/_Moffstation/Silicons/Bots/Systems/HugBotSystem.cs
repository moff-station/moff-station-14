using Content.Server._Moffstation.NPC.HTN.PrimitiveTasks.Operators.Specific;
using Content.Server._Moffstation.Silicons.Bots.Components;
using Content.Shared.Emag.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Moffstation.Silicons.Bots.Systems;

/// <summary>
/// This system manages the "lifecycle" of <see cref="RecentlyHuggedByHugBotComponent"/> and emagging HugBots.
/// </summary>
public sealed class HugBotSystem : EntitySystem
{
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HugBotComponent, HTNRaisedEvent>(OnHtnRaisedEvent);
        SubscribeLocalEvent<HugBotComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnHtnRaisedEvent(Entity<HugBotComponent> entity, ref HTNRaisedEvent args)
    {
        if (args.Data is not HugBotHugEvent ||
            args.Target is not {} target)
            return;

        ApplyHugBotCooldown(entity, target);
    }

    private void OnEmagged(Entity<HugBotComponent> entity, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction) ||
            _emag.CheckFlag(entity, EmagType.Interaction) ||
            !TryComp<HugBotComponent>(entity, out var hugBot))
            return;

        // HugBot HTN checks for emag state within its own logic, so we don't need to change anything here.

        args.Handled = true;
    }

    /// <summary>
    /// Applies <see cref="RecentlyHuggedByHugBotComponent"/> to <paramref name="target"/> based on the configuration of
    /// <paramref name="hugBot"/>.
    /// </summary>
    public void ApplyHugBotCooldown(Entity<HugBotComponent> hugBot, EntityUid target)
    {
        var hugged = EnsureComp<RecentlyHuggedByHugBotComponent>(target);
        hugged.CooldownCompleteAfter = _gameTiming.CurTime + hugBot.Comp.HugCooldown;
    }

    public override void Update(float frameTime)
    {
        // Iterate through all RecentlyHuggedByHugBot entities...
        var huggedEntities = AllEntityQuery<RecentlyHuggedByHugBotComponent>();
        while (huggedEntities.MoveNext(out var huggedEnt, out var huggedComp))
        {
            // ... and if their cooldown is complete...
            if (huggedComp.CooldownCompleteAfter <= _gameTiming.CurTime)
            {
                // ... remove it, allowing them to receive the blessing a hugs once more.
                RemComp<RecentlyHuggedByHugBotComponent>(huggedEnt);
            }
        }
    }
}

[Serializable, DataDefinition]
public sealed partial class HugBotHugEvent : EntityEventArgs;
