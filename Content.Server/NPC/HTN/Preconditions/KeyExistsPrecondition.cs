using Content.Shared.Emag.Systems;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks for the presence of the value by the specified <see cref="KeyExistsPrecondition.Key"/> in the <see cref="NPCBlackboard"/>.
/// Returns true if there is a value.
/// </summary>
public sealed partial class KeyExistsPrecondition : HTNPrecondition
{
    [DataField(required: true), ViewVariables]
    public string Key = string.Empty;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        return blackboard.ContainsKey(Key);
    }
}

// TODO CENT Move this
public sealed partial class IsEmaggedPrecondition : HTNPrecondition
{
    private EmagSystem _emag;

    [DataField]
    public EmagType EmagType = EmagType.Interaction;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _emag = sysManager.GetEntitySystem<EmagSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        return _emag.CheckFlag(owner, EmagType);
    }
}
