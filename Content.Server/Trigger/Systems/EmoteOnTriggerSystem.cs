using Content.Server.Chat.Systems;
using Content.Shared._Moffstation.Extensions;
using Content.Shared.Chat;
using Content.Shared.Random.Helpers;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Trigger.Systems;

public sealed partial class EmoteOnTriggerSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmoteOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<EmoteOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Handled ||
            args.Key != null && !ent.Comp.KeysIn.Contains(args.Key) ||
            (ent.Comp.TargetUser ? args.User : ent.Owner) is not { } target)
            return;

        string message;
        if (ent.Comp.Text is { } t)
        {
            message = Loc.GetString(t);
        }
        else if (ent.Comp.Pack is { } pack && _prototypeManager.Resolve(pack, out var messagePack))
        {
            message = _random.Pick(messagePack);
        }
        else
        {
            this.AssertOrLogError(
                $"{ToPrettyString(ent)}'s {nameof(EmoteOnTriggerComponent)} specifies neither a pack nor text."
            );
            return;
        }

        _chat.TrySendInGameICMessage(target, message, InGameICChatType.Emote, true);
        args.Handled = true;
    }
}
