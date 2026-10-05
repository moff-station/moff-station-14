using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Starfall.Particles;

/// <summary>
/// Sent from the server to nearby clients when an entity is gibbed,
/// triggering a blood-mist particle burst tinted to the entity's blood color.
/// </summary>
[Serializable, NetSerializable]
public sealed class GibMistParticleEvent(MapCoordinates coords, Color bloodColor) : EntityEventArgs
{
    public MapCoordinates Coords = coords;
    public Color BloodColor = bloodColor;
}

