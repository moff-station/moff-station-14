using Content.Shared._Moffstation.Medical.CryoCapsule;
using Content.Shared.Body;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Body.Components;

// todo : this need to be done using DamageSpecifier in some way.
// todo : can probably be done using Perishable !

/// <summary>
/// This is used for organs that can be extracted and used inside another <see cref="BodyComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class ReusableOrganComponent : Component
{
    /// <summary>
    /// Indicate the state of conservation of an organ.
    /// </summary>
    [DataField]
    public int Health = 100;

    /// <summary>
    /// The Health threshold which make an organ go to Healthy, Damaged and Unusable state.
    /// </summary>
    [DataField]
    public int[] Thresholds = [100, 60, 20];

    /// <summary>
    /// Indicate the specie this organ originate from.
    /// </summary>
    [DataField]
    public ProtoId<SpeciesPrototype> Specie;

    /// <summary>
    /// Indicate the specie for which this organ can be used.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> Compatible;
}



[Serializable, NetSerializable]

public record struct OrganEntry(string Specie, ProtoId<OrganCategoryPrototype>? Category, OrganEntry.OrganStatus Status)
{
    public enum OrganStatus { Absent, Unusable, Damaged, Healthy }

    public readonly string Specie = Specie;
    public readonly ProtoId<OrganCategoryPrototype>? Category = Category;
    public OrganStatus Status = Status;
}

