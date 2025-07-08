using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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
    [DataField, ReadOnly(true)]
    public Queue<EntityTableSelector> RewardSpawnsQueue = new();

    /// <summary>
    /// Player info that needs to be stored between their individual lives.
    /// </summary>
    [ViewVariables]
    public Dictionary<NetUserId, GunGamePlayerTrackingInfo> PlayerInfo = new();

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

    /// <summary>
    /// The range to delete casings around a player when they either die or gain their next weapon.
    /// </summary>
    [DataField]
    public float CasingDeletionRange = 1.0f;

    /// <summary>
    /// The probability that a casing will be deleted when a player either dies or gains their next weapon.
    /// This is largely to still leave a handful of casings around on the map for flavor while
    /// still ensuring we remove most of them.
    /// </summary>
    [DataField]
    public float CasingDeletionProb = 0.8f;

    /// <summary>
    /// When this gamerule spawns an energy weapon, it will try to
    /// upgrade it to have at least this weapon recharge rate.
    /// </summary>
    [DataField]
    public float DefaultEnergyWeaponRechargeRate = 30.0f;
}

/// <summary>
/// The info for every player we're tracking during the round.
/// </summary>
[Serializable]
public struct GunGamePlayerTrackingInfo(NetUserId userId, Queue<EntityTableSelector> rewardQueue)
{
    public Queue<EntityTableSelector> RewardQueue = rewardQueue;
    public int Kills = 0;
    public NetUserId UserId = userId; // we store this just so it's easy to pass it around
}
