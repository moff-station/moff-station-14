using Content.Shared.Silicons.Laws;

namespace Content.Shared._Moffstation.Silicons.LawReprogrammer;

/// <summary>
/// This handles...
/// </summary>
public sealed class SharedLawReprogrammerSystem : EntitySystem
{
    [Dependency] private readonly SharedSiliconLawSystem _lawSystem = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<>();
    }

    private void OnUse()
    {
        
    }

}
