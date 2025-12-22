using Content.Shared._Moffstation.CartridgeLoader.Cartridges;
using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Shared._Moffstation.NanoChat;

/// <summary>
///     Base system for NanoChat functionality shared between client and server.
/// </summary>
public abstract class SharedNanoChatSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NanoChatCardComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<NanoChatCardComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.Number == null)
        {
            args.PushMarkup(Loc.GetString("nanochat-card-examine-no-number"));
            return;
        }

        args.PushMarkup(Loc.GetString("nanochat-card-examine-number", ("number", $"{ent.Comp.Number:D4}")));
    }

    #region Public API Methods

    /// <summary>
    ///     Gets the NanoChat number for a card.
    /// </summary>
    public uint? GetNumber(Entity<NanoChatCardComponent?> card)
    {
        if (!Resolve(card, ref card.Comp))
            return null;

        return card.Comp.Number;
    }

    /// <summary>
    ///     Sets the NanoChat number for a card.
    /// </summary>
    public void SetNumber(Entity<NanoChatCardComponent?> card, uint number)
    {
        if (!Resolve(card, ref card.Comp))
            return;

        card.Comp.Number = number;
        Dirty(card);
    }

    /// <summary>
    ///     Sets a specific recipient in the card.
    /// </summary>
    public void SetRecipient(Entity<NanoChatCardComponent?> card, uint number, NanoChatRecipient recipient)
    {
        if (!Resolve(card, ref card.Comp))
            return;

        card.Comp.Recipients[number] = recipient;
        Dirty(card);
    }

    /// <summary>
    ///     Gets the currently selected chat recipient.
    /// </summary>
    public uint? GetCurrentChat(Entity<NanoChatCardComponent?> card)
    {
        if (!Resolve(card, ref card.Comp))
            return null;

        return card.Comp.CurrentChat;
    }

    /// <summary>
    ///     Sets the currently selected chat recipient.
    /// </summary>
    public void SetCurrentChat(Entity<NanoChatCardComponent?> card, uint? recipient)
    {
        if (!Resolve(card, ref card.Comp))
            return;

        card.Comp.CurrentChat = recipient;
        Dirty(card);
    }

    /// <summary>
    ///     Gets whether notifications are muted.
    /// </summary>
    public bool GetNotificationsMuted(Entity<NanoChatCardComponent?> card)
    {
        if (!Resolve(card, ref card.Comp))
            return false;

        return card.Comp.NotificationsMuted;
    }

    /// <summary>
    ///     Sets whether notifications are muted.
    /// </summary>
    public void SetNotificationsMuted(Entity<NanoChatCardComponent?> card, bool muted)
    {
        if (!Resolve(card, ref card.Comp))
            return;

        card.Comp.NotificationsMuted = muted;
        Dirty(card);
    }

    /// <summary>
    ///     Gets whether NanoChat number is listed.
    /// </summary>
    public bool GetListNumber(Entity<NanoChatCardComponent?> card)
    {
        if (!Resolve(card, ref card.Comp))
            return false;

        return card.Comp.ListNumber;
    }

    /// <summary>
    ///     Sets whether NanoChat number is listed.
    /// </summary>
    public void SetListNumber(Entity<NanoChatCardComponent?> card, bool listNumber)
    {
        if (!Resolve(card, ref card.Comp) || card.Comp.ListNumber == listNumber)
            return;

        card.Comp.ListNumber = listNumber;
        Dirty(card);
    }

    /// <summary>
    ///     Gets if there are unread messages from a recipient.
    /// </summary>
    public bool HasUnreadMessages(Entity<NanoChatCardComponent?> card, uint recipientNumber)
    {
        if (!Resolve(card, ref card.Comp) || !card.Comp.Recipients.TryGetValue(recipientNumber, out var recipient))
            return false;

        return recipient.HasUnread;
    }
#endregion
}
