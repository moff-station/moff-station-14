using System.Collections.Generic;
using System.Linq;
using Content.Shared._Moffstation.Cards;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Client._Moffstation.Cards;

public sealed class CardSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;
    private readonly Dictionary<EntityUid, bool> _lastFaceUp = new();

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("cards");
        _sawmill.Info("Client CardSystem.Initialize");

        SubscribeLocalEvent<CardComponent, ComponentInit>(OnCardInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            var current = card.IsFaceUp;
            if (!_lastFaceUp.TryGetValue(uid, out var last) || last != current)
            {
                _sawmill.Info($"Client detected flip: uid={uid}, IsFaceUp={current}");
                _lastFaceUp[uid] = current;
                UpdateCardVisual(uid, card);
            }
        }
    }

    private void OnCardInit(EntityUid uid, CardComponent card, ComponentInit args)
    {
        _sawmill.Info($"Client OnCardInit: uid={uid}, IsFaceUp={card.IsFaceUp}, frontRsi={card.FrontRsi}, backRsi={card.BackRsi}");
        _lastFaceUp[uid] = card.IsFaceUp;
        UpdateCardVisual(uid, card);
    }

    private void UpdateCardVisual(EntityUid uid, CardComponent card)
    {
        if (!EntityManager.TryGetComponent(uid, out SpriteComponent? sprite))
        {
            _sawmill.Warning($"UpdateCardVisual: no SpriteComponent on {uid}");
            return;
        }

        var (rsiPath, state) = GetVisual(card, uid);

        _sawmill.Info($"UpdateCardVisual: uid={uid}, IsFaceUp={card.IsFaceUp}, rsi={(rsiPath ?? "<same>")}, state={state}");

        if (!sprite.AllLayers.Any())
        {
            if (!string.IsNullOrEmpty(rsiPath))
                sprite.AddLayer(new SpriteSpecifier.Rsi(new ResPath(rsiPath), state));
            else
                sprite.AddLayer(state);
            return;
        }

        if (!string.IsNullOrEmpty(rsiPath))
            sprite.LayerSetSprite(0, new SpriteSpecifier.Rsi(new ResPath(rsiPath), state));
        else
            sprite.LayerSetState(0, state);
    }

    private (string? rsiPath, string state) GetVisual(CardComponent card, EntityUid uid)
    {
        if (card.HeadSlotPrivacy && IsCardInHeadSlot(uid))
        {
            if (!string.IsNullOrEmpty(card.BackRsi) || !string.IsNullOrEmpty(card.BackState))
            {
                var rsi = !string.IsNullOrEmpty(card.BackRsi) ? card.BackRsi : null;
                var state = !string.IsNullOrEmpty(card.BackState) ? card.BackState! : "blank";
                return (rsi, state);
            }

            return (null, "blank");
        }

        if (!card.IsFaceUp)
        {
            if (!string.IsNullOrEmpty(card.BackRsi) || !string.IsNullOrEmpty(card.BackState))
            {
                var rsi = !string.IsNullOrEmpty(card.BackRsi) ? card.BackRsi : null;
                var state = !string.IsNullOrEmpty(card.BackState) ? card.BackState! : "back";
                return (rsi, state);
            }

            return (null, "back");
        }

        if (!string.IsNullOrEmpty(card.FrontRsi) || !string.IsNullOrEmpty(card.FrontState))
        {
            var rsi = !string.IsNullOrEmpty(card.FrontRsi) ? card.FrontRsi : null;
            var state = !string.IsNullOrEmpty(card.FrontState)
                ? card.FrontState!
                : NormalizeValueToState(card.Value);

            return (rsi, state);
        }

        var frontState = NormalizeValueToState(card.Value);
        return (null, frontState);
    }

    private string NormalizeValueToState(string value)
    {
        var v = value.ToLowerInvariant();
        return v switch
        {
            "a" or "ace"        => "ace",
            "2" or "two"        => "two",
            "3" or "three"      => "three",
            "4" or "four"       => "four",
            "5" or "five"       => "five",
            "6" or "six"        => "six",
            "7" or "seven"      => "seven",
            "8" or "eight"      => "eight",
            "9" or "nine"       => "nine",
            "10" or "ten"       => "ten",
            "j" or "jack"       => "jack",
            "q" or "queen"      => "queen",
            "k" or "king"       => "king",
            "blank"             => "blank",
            "joker" or "joker1" => "joker1",
            "joker2"            => "joker2",
            _                   => "blank"
        };
    }

    private bool IsCardInHeadSlot(EntityUid uid) => false;
}
