using Content.Shared._Moffstation.Medical.CryoCapsule;
using Content.Shared.Body;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.Body.Components;

// todo : this need to be done using DamageSpecifier in some way.

/// <summary>
/// This is used for organs that can be conserved and used with the <see cref="CryoCapsuleComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class ConservableOrganComponent : Component
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
    /// From what specie does this organ come from
    /// </summary>
    [DataField]
    public ProtoId<SpeciesPrototype> Specie;

    /// <summary>
    /// The specie that can use this organ as a substitute
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>> Compatible;
}


/// <summary>
/// Contain informations about an organ.
/// TODO : this will probably be put in another component, with files related to organs.
/// </summary>
[Serializable, NetSerializable]

public record struct OrganEntry(string Name, string Specie, ProtoId<OrganCategoryPrototype> Category, OrganEntry.OrganStatus Status)
{
    public enum OrganStatus { Absent, Unusable, Damaged, Healthy }

    public readonly string Name = Name;
    public readonly string Specie = Specie;
    public readonly ProtoId<OrganCategoryPrototype> Category = Category;
    public OrganStatus Status = Status;
}

