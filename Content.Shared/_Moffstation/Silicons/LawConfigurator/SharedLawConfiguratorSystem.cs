namespace Content.Shared._Moffstation.Silicons.LawConfigurator;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class SharedLawConfiguratorSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeConfigurator();
        InitializeTarget();
    }

    // TODO : introduce some code communicating with the server side to access the laws inside the card.
}
