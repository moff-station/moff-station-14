// these are HEAVILY based on the Bingle free-agent ghostrole from GoobStation, but reflavored and reprogrammed to make them more Robust (and less of a meme.)
// all credit for the core gameplay concepts and a lot of the core functionality of the code goes to the folks over at Goob, but I re-wrote enough of it to justify putting it in our filestructure.
// the original Bingle PR can be found here: https://github.com/Goob-Station/Goob-Station/pull/1519

using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Replicator;

public abstract class SharedReplicatorNestSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public EntityUid? UpgradeReplicator(Entity<ReplicatorComponent> ent, EntProtoId nextStage)
    {
        if (!_mind.TryGetMind(ent, out var mind, out _))
            return null;

        var xform = Transform(ent);

        var upgraded = Spawn(nextStage, xform.Coordinates);
        var upgradedComp = EnsureComp<ReplicatorComponent>(upgraded);
        upgradedComp.RelatedReplicators = ent.Comp.RelatedReplicators;
        upgradedComp.MyNest = ent.Comp.MyNest;

        if (ent.Comp.MyNest != null)
        {
            var nestComp = EnsureComp<ReplicatorNestComponent>((EntityUid)ent.Comp.MyNest);
            nestComp.SpawnedMinions.Remove(ent);
            nestComp.SpawnedMinions.Add(upgraded);

            _audio.PlayPvs(nestComp.UpgradeSound, upgraded);
        }

        _mind.TransferTo(mind, upgraded);

        _popup.PopupEntity(Loc.GetString($"{ent.Comp.ReadyToUpgradeMessage}-self"), upgraded, PopupType.Medium);

        return upgraded;
    }
}

public sealed partial class ReplicatorSpawnNestActionEvent : InstantActionEvent
{

}

public sealed partial class ReplicatorUpgradeActionEvent : InstantActionEvent
{
    [DataField(required: true)]
    public EntProtoId NextStage;
}

[ByRefEvent]
public sealed partial class ReplicatorNestEmbiggenedEvent(Entity<ReplicatorNestComponent> ent) : EntityEventArgs
{
    public Entity<ReplicatorNestComponent> Ent { get; set; } = ent;
}
