using Content.Shared._Moffstation.Chitter;
using Content.Shared.Access.Components;
using Robust.Shared.Serialization;

// ReSharper disable once CheckNamespace // Moff addition to upstream class.
namespace Content.Shared.Access.Systems;

public abstract partial class SharedAgentIdCardSystem
{
    [SubscribeLocalEvent]
    private void OnNumberChanged(Entity<AgentIDCardComponent> ent, ref AgentIDCardNumberChangedMessage args) =>
        UpdateUi(ent);
}

[Serializable, NetSerializable]
// ReSharper disable once InconsistentNaming // Cursed by upstream's conventions.
public sealed class AgentIDCardNumberChangedMessage(ChitterAccountId id) : BoundUserInterfaceMessage
{
    public readonly ChitterAccountId Id = id;
}
