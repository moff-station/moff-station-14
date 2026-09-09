using Content.Server.Antag;
using Content.Shared._Moffstation.CharacterMenu;
using Content.Shared._Moffstation.Objectives;
using Content.Shared.Mind;
using Robust.Shared.Player;

namespace Content.Server._Moffstation.CharacterMenu;

/// <summary>
/// For opening the character UI on people. LOOK AT MY AWESOME UI!!!!
/// </summary>
public sealed partial class OpenCharacterMenuSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _player = default!;

    [SubscribeLocalEvent]
    private void OnAntagSelected(ref AfterAntagEntitySelectedEvent ev)
    {
        if (ev.Session is { } session)
            RaiseNetworkEvent(new OpenCharacterMenuEvent(), session.Channel);
    }

    [SubscribeLocalEvent]
    private void OnObjectiveAdded(ref ObjectiveAddedEvent ev)
    {
        if (TryComp<MindComponent>(ev.Mind, out var mind) && _player.TryGetSessionById(mind.UserId, out var session))
            RaiseNetworkEvent(new OpenCharacterMenuEvent(), session.Channel);
    }
}
