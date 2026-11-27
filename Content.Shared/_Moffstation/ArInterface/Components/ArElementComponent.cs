using System.Numerics;
using Content.Shared._Moffstation.Overlay.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Moffstation.ArInterface.Components;

/// <summary>
/// Mark an entity as being part of an augmented reality setting.
/// </summary>
[RegisterComponent]
public sealed partial class ArElementComponent : Component
{
    /// <summary>
    /// The augmented reality interface this entity belong to
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ArInterfacePrototype> Interface;

    /// <summary>
    /// Indicate where to place the entity when initialised.
    /// </summary>
    [DataField(required: true)]
    public IArElementSpawnStrategy SpawnAt;
}

public interface IArElementSpawnStrategy;


/// <summary>
/// Parent the element to the entity from which it spawned
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ArSpawnAtSource : IArElementSpawnStrategy
{
    [DataField]
    public Vector2 Offset;

    [DataField]
    public float Angle;
}


/// <summary>
/// Parent the element to the entity viewer the interface
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ArSpawnAtViewer : IArElementSpawnStrategy
{
    [DataField]
    public Vector2 Offset;

    [DataField]
    public float Angle;
}
