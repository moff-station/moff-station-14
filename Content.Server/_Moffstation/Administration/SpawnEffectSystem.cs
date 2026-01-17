using Robust.Shared.Enums;
using Robust.Shared.Placement;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.Administration;

public sealed class SpawnEffectSystem : EntitySystem
{
    // Store active effects per user
    private readonly Dictionary<NetUserId, EntProtoId> _activeEffects = new();
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    public override void Initialize()
    {

        base.Initialize();
        SubscribeLocalEvent<PlacementEntityEvent>(OnPlace);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }
    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged; // Wawa unsubscribe on shutdown
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
        _activeEffects.Remove(user);
    }
    public void SetEffect(NetUserId user, [ForbidLiteral] EntProtoId effectId)
    {
        if (!_activeEffects.ContainsKey(user))
        {
            _activeEffects.Add(user, effectId);
        }
        _activeEffects[user] = effectId;

    }

    private void OnPlace(PlacementEntityEvent args)
    {
        // You have to be "creating something" and need to be a player cause server cant place stuff wawa
        if (args.PlacementEventAction != PlacementEventAction.Create || args.PlacerNetUserId == null)
            return;

        if (_activeEffects.TryGetValue(args.PlacerNetUserId.Value, out var effect))
        {
            Spawn(effect, args.Coordinates);
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
        {
            _activeEffects.Remove(e.Session.UserId);
        }
    }
}
