using Content.Server.Sandbox;

namespace Content.Server._Moffstation.Administration;

public sealed class SpawnEffectSystem : EntitySystem
{
    // What to effect to spawn, when null its disabled
    public string? ActiveEffect { get; set; }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransformComponent ,MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<TransformComponent> coord, ref MapInitEvent args)
    {
        if (ActiveEffect == null || !SandboxSystem.IsPlacementProcessing)
            return;

        SandboxSystem.IsPlacementProcessing = false;

        Spawn(ActiveEffect, coord.Comp.Coordinates);
    }
}
