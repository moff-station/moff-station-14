using Content.Shared.EntityEffects;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation;

/// A component which adds <see cref="AlternativeVerb"/>(s) to its owner which invoke <see cref="EntityEffect"/>s on use.
[RegisterComponent, Access(typeof(EffectiveAlternativeVerbsSystem))]
public sealed partial class EffectiveAlternativeVerbsComponent : Component
{
    [DataField]
    public List<EffectiveVerbCategory> Categories = new();
}

/// A grouping of <see cref="EffectiveVerb"/> which renders as a group in the verb menu.
[DataRecord]
public partial record struct EffectiveVerbCategory
{
    public LocId Text;
    public ResPath? Icon;
    public List<EffectiveVerb> Options;
}

/// A single verb in a <see cref="EffectiveAlternativeVerbsComponent"/>. One verb can invoke many effects.
[DataRecord]
public partial record struct EffectiveVerb
{
    public LocId Text;
    public SpriteSpecifier? Icon;
    public List<EffectiveVerbEffect> Effects;
}

/// A single effect invoked by an <see cref="EffectiveVerb"/>.
[DataRecord]
public partial record struct EffectiveVerbEffect
{
    public EntityEffect Effect;
    public bool ApplyToUser;
}
