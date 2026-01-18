using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Placement;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Administration;

public sealed class SpawnEffectSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly Dictionary<NetUserId, EntProtoId> _activeEffectsByUser = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlacementEntityEvent>(OnPlace);

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public bool TrySetEffect(NetUserId user, string? effectId)
    {
        if (effectId is null) {
            ClearEffect(user);
            return true;
        }

        if (!_proto.HasIndex<EntityPrototype>(effectId))
            return false;

        SetEffect(user, effectId);
        return true;
    }

    public void ClearEffect(NetUserId user)
    {
        _activeEffectsByUser.Remove(user);
    }

    public void SetEffect(NetUserId user, [ForbidLiteral] EntProtoId effectId)
    {
        if (!_activeEffectsByUser.ContainsKey(user))
        {
            _activeEffectsByUser.Add(user, effectId);
        }
        _activeEffectsByUser[user] = effectId;

    }

    public IEnumerable<CompletionOption> GetEffects()
    {
        return _proto.EnumeratePrototypes<EntityPrototype>()
            .Where(p => p.ID.StartsWith("AdminInstantEffect"))
            .Select(p => new CompletionOption(p.ID, p.Name))
            .OrderBy(o => o.Value);
    }

    private void OnPlace(PlacementEntityEvent args)
    {
        // You have to be "creating something" and need to be a player cause server cant place stuff wawa
        if (args.PlacementEventAction != PlacementEventAction.Create || args.PlacerNetUserId == null)
            return;

        if (_activeEffectsByUser.TryGetValue(args.PlacerNetUserId.Value, out var effect))
        {
            Spawn(effect, args.Coordinates);
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
        {
            _activeEffectsByUser.Remove(e.Session.UserId);
        }
    }
}
