using Content.Shared.CCVar;
using Content.Shared.Chat;  // Moffstation
using Content.Shared.Chat.TypingIndicator;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes; // Moffstation
using Robust.Shared.Timing;

namespace Content.Client.Chat.TypingIndicator;

// Client-side typing system tracks user input in chat box
public sealed class TypingIndicatorSystem : SharedTypingIndicatorSystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly TimeSpan _typingTimeout = TimeSpan.FromSeconds(2);
    private TimeSpan _lastTextChange;
    private bool _isClientTyping;
    private bool _isClientChatFocused;
    private ProtoId<TypingIndicatorPrototype>? _channelIndicator; // Moffstation - Typing indicators

    // Moffstation - Start - Typing indicators
    private static readonly ProtoId<TypingIndicatorPrototype> EmoteIndicator = "emote";
    private static readonly ProtoId<TypingIndicatorPrototype> OocIndicator = "ooc";
    private static readonly ProtoId<TypingIndicatorPrototype> RadioIndicator = "radio";
    private static readonly ProtoId<TypingIndicatorPrototype> WhisperIndicator = "whisper";
    // Moffstation - End

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, CCVars.ChatShowTypingIndicator, OnShowTypingChanged);
    }

    public void ClientChangedChatText()
    {
        // don't update it if player don't want to show typing indicator
        if (!_cfg.GetCVar(CCVars.ChatShowTypingIndicator))
            return;

        // client typed something - show typing indicator
        _isClientTyping = true;
        ClientUpdateTyping();
        _lastTextChange = _time.CurTime;
    }

    public void ClientSubmittedChatText()
    {
        // don't update it if player don't want to show typing
        if (!_cfg.GetCVar(CCVars.ChatShowTypingIndicator))
            return;

        // client submitted text - hide typing indicator
        _isClientTyping = false;
        ClientUpdateTyping();
    }

    public void ClientChangedChatFocus(bool isFocused)
    {
        // don't update it if player don't want to show typing
        if (!_cfg.GetCVar(CCVars.ChatShowTypingIndicator))
            return;

        // client submitted text - hide typing indicator
        _isClientChatFocused = isFocused;
        ClientUpdateTyping();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_time.IsFirstTimePredicted)
            return;

        // check if client didn't changed chat text box for a long time
        if (_isClientTyping)
        {
            var dif = _time.CurTime - _lastTextChange;
            if (dif > _typingTimeout)
            {
                // client didn't typed anything for a long time - change indicator
                _isClientTyping = false;
                ClientUpdateTyping();
            }
        }
    }

    private void ClientUpdateTyping()
    {
        // check if player controls any pawn
        if (_playerManager.LocalEntity == null)
            return;

        var state = TypingIndicatorState.None;
        if (_isClientChatFocused)
            state = _isClientTyping ? TypingIndicatorState.Typing : TypingIndicatorState.Idle;

        // send a networked event to server
        RaisePredictiveEvent(new TypingChangedEvent(state, _channelIndicator)); // Moffstation - Typing indicators
    }

    private void OnShowTypingChanged(bool showTyping)
    {
        // hide typing indicator immediately if player don't want to show it anymore
        if (!showTyping)
        {
            _isClientTyping = false;
            ClientUpdateTyping();
        }
    }

    // Moffstation - Start - Typing Indicators
    // A table for overriding the normal indicator based on the selected channel
    private static ProtoId<TypingIndicatorPrototype>? ChannelSelectIndicator(ChatSelectChannel channel)
    {
        return channel switch
        {
            // Switch statement is stupid and doesn't properly infer its nullable
            // ReSharper disable once RedundantCast
            ChatSelectChannel.Emotes => (ProtoId<TypingIndicatorPrototype>?)EmoteIndicator,
            ChatSelectChannel.LOOC => OocIndicator,
            ChatSelectChannel.OOC => OocIndicator,
            ChatSelectChannel.Radio => RadioIndicator,
            ChatSelectChannel.Whisper => WhisperIndicator,
            _ => null,
        };
    }

    public void UpdateChannelIndicator(ChatSelectChannel channel)
    {
        _channelIndicator = ChannelSelectIndicator(channel);
        ClientUpdateTyping();
    }
    // Moffstation - End
}
