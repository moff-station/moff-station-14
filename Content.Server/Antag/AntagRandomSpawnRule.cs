using Content.Server.Antag.Components;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules;

namespace Content.Server.Antag;

public sealed class AntagRandomSpawnSystem : GameRuleSystem<AntagRandomSpawnComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagRandomSpawnComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    protected override void Added(EntityUid uid, AntagRandomSpawnComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, comp, gameRule, args);

        // we have to select this here because AntagSelectLocationEvent is raised twice because MakeAntag is called twice
        // once when a ghost role spawner is created and once when someone takes the ghost role

        if (TryFindRandomTile(out _, out _, out _, out var coords))
            comp.Coords = coords;
    }

    // Moffstation - Start - Rewrote this function to double check coords are filled
    // (if you use the PrePlayerSpawn for the rule it would spawn you in nullspace)
    // If upstream updates for that or fixes it, probably go with what they did
    private void OnSelectLocation(Entity<AntagRandomSpawnComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords is not { } coords)
        {
            if (!TryFindRandomTile(out _, out _, out _, out coords))
                return;
        }

        args.Coordinates.Add(_transform.ToMapCoordinates(coords));
    }
    // Moffstation - End
}
