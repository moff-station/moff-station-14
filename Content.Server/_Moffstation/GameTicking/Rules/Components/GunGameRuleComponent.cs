using System.ComponentModel;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._Moffstation.GameTicking.Rules.Components;

/// <summary>
/// A game rule which rewards players with a new weapon sequentially and
/// ends once someone gets the required number of kills with their final weapon.
/// </summary>

[RegisterComponent]
public sealed partial class GunGameRuleComponent : Robust.Shared.GameObjects.Component
{
    /// <summary>
    /// How long until the round restarts
    /// </summary>
    [DataField]
    public TimeSpan RestartDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The person who won.
    /// We store this here in case of some assist shenanigans.
    /// </summary>
    [DataField]
    public NetUserId? Victor;

    /// <summary>
    /// The queue to use when spawning weapons.
    /// This queue is replicated for each player and every respective player receives the next
    /// item in the queue when they get a kill.
    /// </summary>
    [DataField]
    public Queue<EntityTableSelector> RewardSpawnsQueue = new();

    /// <summary>
    /// Individual player reward queues, copied from the reward spawns queue.
    /// </summary>
    [DataField, ReadOnly(true)]
    public Dictionary<NetUserId, Queue<EntityTableSelector>> PlayerRewards = new();

    /// <summary>
    /// Individual player kills at their current position in the queue
    /// </summary>
    [DataField]
    public Dictionary<NetUserId, int> PlayerKills = new();

    /// <summary>
    /// The amount of kills a player has to get before they are able to receive a new weapon.
    /// </summary>
    [DataField, ReadOnly(true)]
    public int KillsPerWeapon = 1;

    /// <summary>
    /// When spawning gear for people after they get a kill
    /// this is the order of equip slots to try spawning items into.
    /// The slots will be tried in order per item in the current spot in the queue.
    /// If an item doesn't fit into a slot, that slot will be ignored for the rest of them.
    /// </summary>
    [DataField, ReadOnly(true)]
    public Queue<string> SlotTryOrder = new();

    /// <summary>
    /// The gear all players spawn with.
    /// This loadout should not include a starting weapon as the starting weapon is selected from the
    /// <see cref="RewardSpawnsQueue"/>.
    /// </summary>
    [DataField, ReadOnly(true)]
    public ProtoId<StartingGearPrototype> Gear = "GunGameGear";
}
