using System.Diagnostics.CodeAnalysis;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Robotics.LawProgrammer;

/// <summary>
/// This handles the law programmer
/// </summary>
public sealed partial class SharedLawProgrammerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeConfigurator();
        InitializeTarget();
    }

    // this is an evil way to do this but i just don't know how to do it the correct way
    // (i.e go fetch the lawset on the server side then get it back in shared)
    private bool TryGetBoardLaws(Entity<LawProgrammerComponent> ent, [NotNullWhen(true)] out List<SiliconLaw>? laws)
    {
        laws = null;
        if (!_container.TryGetContainer(ent, ent.Comp.LawBoardSlot, out var container) ||
            container.ContainedEntities.Count == 0)
            return false;

        var board = container.ContainedEntities[0];

        if (!_entMan.TryGetComponent<SiliconLawProviderComponent>(board, out var provider))
            return false;

        laws = _lawSystem.GetLawset(provider.Laws).Laws;
        return true;
    }
}


[ByRefEvent]
public sealed class BeforeProgramAttemptEvent(EntityUid user, EntityUid used, TimeSpan initialTime, bool needMind) : EntityEventArgs
{
    public bool CanProceed = false;
    public readonly EntityUid User = user;
    public readonly EntityUid Used = used;
    public readonly bool NeedMind = needMind;
    public TimeSpan Time = initialTime;
}


[Serializable, NetSerializable]
public sealed partial class ProgramAttemptDoAfterEvent: DoAfterEvent
{
    public readonly List<SiliconLaw> Laws;
    public readonly bool NeedMind;
    public bool CancelHandled;

    public ProgramAttemptDoAfterEvent(List<SiliconLaw> laws, bool needMind)
    {
        Laws = laws;
        NeedMind = needMind;
        CancelHandled = false;
    }

    public override DoAfterEvent Clone() => this;
}

