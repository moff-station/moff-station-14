using Content.Server.Sandbox;
using Robust.Shared.Placement;
using Robust.Shared.Network;
using System.Collections.Generic;

namespace Content.Server._Moffstation.Administration;

public sealed class SpawnEffectSystem : EntitySystem
{
    // Store active effects per user
    private readonly Dictionary<NetUserId, string> _activeEffects = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlacementEntityEvent>(OnPlace);
    }

    public void SetActiveEffect(NetUserId user, string? effect)
    {
        if (effect == null)
        {
            _activeEffects.Remove(user);
            return;
        }

        _activeEffects[user] = effect;
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
}
