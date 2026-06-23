using Content.Server.GameTicking;
using Content.Shared._Moffstation.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;

namespace Content.Server._Moffstation.Discord.GuildEvent;

public sealed partial class DiscordGuildEventSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private DiscordGuildEventManager _eventManager = default!;
    [Dependency] private GameTicker _gameTicker = default!;

    private bool _discordEventEnabled;
    private string _currentEventName = "";
    private string _currentEventDescription = "";
    private string _currentEventLocation = "";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<TickerLobbyCountdownEvent>(OnDelayChanged);

        Subs.CVar(_cfg, MoffCCVars.DiscordRoundEventEnabled, value => _discordEventEnabled = value, true);
        Subs.CVar(_cfg, MoffCCVars.DiscordRoundEventName, value => _currentEventName = value, true);
        Subs.CVar(_cfg, MoffCCVars.DiscordRoundEventDescription, value => _currentEventDescription = value, true);
        Subs.CVar(_cfg, MoffCCVars.DiscordRoundEventLocation, value => _currentEventLocation = value, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _eventManager.Shutdown();
    }

    private void OnDelayChanged(TickerLobbyCountdownEvent args)
    {
        if (!_discordEventEnabled)
            return;

        if (args.Paused)
            EndActiveEvent();
        else
            EnsureEventActive();
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (!_discordEventEnabled)
            return;

        switch (ev.New)
        {
            case GameRunLevel.InRound:
            case GameRunLevel.PostRound:
            case GameRunLevel.PreRoundLobby when !_gameTicker.Paused:
                EnsureEventActive();
                break;
            case GameRunLevel.PreRoundLobby when _gameTicker.Paused:
                EndActiveEvent();
                break;
        }
    }

    private async void EnsureEventActive()
    {
        await _eventManager.EnsureEventActiveAsync(_currentEventName, _currentEventDescription, _currentEventLocation);
    }

    private async void EndActiveEvent()
    {
        await _eventManager.EndActiveEventAsync();
    }
}
