namespace Content.Server._Moffstation.GameTicking.Rules.Components;

/// Tracks stats about Replicators for the round-end dialog.
[RegisterComponent, Access(typeof(ReplicatorRuleSystem))]
public sealed partial class ReplicatorRuleComponent : Component
{
    public int TotalSizePoints;
    public int TotalSpawnersCreated;
    public int NestCount;
}
