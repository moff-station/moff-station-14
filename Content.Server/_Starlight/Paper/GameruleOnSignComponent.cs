using Content.Shared.GameTicking.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Component = Robust.Shared.GameObjects.Component;

namespace Content.Server._Starlight.Paper;

[RegisterComponent]
public sealed partial class GameruleOnSignComponent : Component
{
    /// <summary>
    /// how many signatures are needed before the gamerules on paper goes into effect.
    /// </summary>
    [DataField]
    public int SignaturesNeeded = 1;

    /// <summary>
    /// How many charges this paper has, in terms of supplying new objectives/antags.
    /// </summary>
    [DataField]
    public int Charges = 1;

    /// <summary>
    /// A list of every entity that has signed this paper to prevent spam signing from instantly activating the paper.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> SignedEntityUids = [];

    /// <summary>
    /// A Whitelist of whos signatures should count for this component.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A Whitelist of whos signatures should not count for this component.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// The chance the associated gamerule triggering after the required signatures are collected.
    /// </summary>
    [DataField]
    public float GameruleChance = 1.0f;

    /// <summary>
    /// The chance of the objectives/antags being applied when this paper is signed
    /// </summary>
    [DataField]
    public float TriggerChance = 1.0f;

    /// <summary>
    /// What game rules are added once signatures are collected and with a bit of luck.
    /// </summary>
    [DataField]
    public List<EntProtoId<GameRuleComponent>> Rules = [];

    /// <summary>
    /// What antags should be applied to signers of the paper
    /// </summary>
    [DataField]
    public List<EntProtoId<GameRuleComponent>> Antags = [];

    /// <summary>
    /// what objectives should be added to the person.
    /// </summary>
    [DataField]
    public List<EntProtoId> Objectives = [];
}
