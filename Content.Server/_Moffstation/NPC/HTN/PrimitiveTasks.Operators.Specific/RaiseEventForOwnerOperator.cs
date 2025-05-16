using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server._Moffstation.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class RaiseEventForOwnerOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    [DataField]
    public string? TargetKey;

    [DataField(required: true)]
    public EntityEventArgs Event;

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        _entMan.EventBus.RaiseLocalEvent(
            blackboard.GetValue<EntityUid>(NPCBlackboard.Owner),
            new HTNRaisedEvent(
                blackboard.GetValue<EntityUid>(NPCBlackboard.Owner),
                TargetKey is { } targetKey ? blackboard.GetValue<EntityUid>(targetKey) : null,
                Event
            )
        );

        return HTNOperatorStatus.Finished;
    }
}

public sealed partial class HTNRaisedEvent(EntityUid owner, EntityUid? target, EntityEventArgs data) : EntityEventArgs
{
    public EntityUid Owner = owner;
    public EntityUid? Target = target;
    public EntityEventArgs Data = data;
}
