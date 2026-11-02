using Content.Shared._Moffstation.Dice;
using Content.Shared._Moffstation.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Dice;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT;

namespace Content.Shared._Moffstation.Xenoarchaeology.Artifact.XAT.Systems;

public sealed partial class XATDiceRollSystem : BaseXATSystem<XATDiceRollComponent>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<XenoArtifactComponent> _xenoArtifactQuery = default!;

    [SubscribeLocalEvent]
    private void OnRoll(Entity<DiceComponent> ent, ref DiceRolledEvent args)
    {
        var targetCoords = Transform(ent).Coordinates;

        var query = EntityQueryEnumerator<XATDiceRollComponent, XenoArtifactNodeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var node))
        {
            if (node.Attached == null)
                continue;

            if (comp.RequiredSides != ent.Comp.Sides && comp.RequiredSides is not null)
                continue;
            if (comp.TriggerValue != ent.Comp.CurrentValue)
                continue;

            var artifact = _xenoArtifactQuery.Get(node.Attached.Value);

            if (!CanTrigger(artifact, (uid, node)))
                continue;

            var artifactCoords = Transform(artifact).Coordinates;
            if (_transform.InRange(targetCoords, artifactCoords, comp.Range))
                Trigger(artifact, (uid, comp, node));
        }
    }
}
