namespace Content.Server._Moffstation.GameTicking.Rules.Components;

/// <summary>
/// This component is given to players at the start of the GunGame gamemode.
/// It is used to keep track of a player's currently spawned weapons.
/// <see cref="Content.Server._Moffstation.GameTicking.Rules.Components.GunGameRuleComponent.RewardSpawnsQueue"/>
/// </summary>

[RegisterComponent]
public sealed partial class GunGameTrackerComponent : Component
{
    /// <summary>
    /// The list of current rewards given to the player.
    /// This is largely so the previous rewards can be deleted after the player gets a kill,
    /// since they should then be given the next weapon in their queue to replace it.
    /// </summary>
    [DataField]
    public List<EntityUid> CurrentRewards = new();
}
