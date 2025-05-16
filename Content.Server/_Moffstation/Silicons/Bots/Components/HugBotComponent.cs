using Content.Server._Moffstation.Silicons.Bots.Systems;

namespace Content.Server._Moffstation.Silicons.Bots.Components;

/// <summary>
/// This component makes its associated entity capable of <see cref="HugBotSystem.ApplyHugBotCooldown">HugBot hugging</see>.
/// </summary>
/// <see cref="HugBotSystem"/>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class HugBotComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan HugCooldown = TimeSpan.FromMinutes(2);
}
