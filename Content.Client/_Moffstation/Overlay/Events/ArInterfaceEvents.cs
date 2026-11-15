using Robust.Client.Graphics;

namespace Content.Client._Moffstation.Overlay.Events;


/// <summary>
/// Raised on an entity when
/// </summary>
/// <param name="SourceEntity">the entity that spawned the entity</param>
/// <param name="ViewerEntity">the entity looking at the interface</param>
[ByRefEvent]
public record struct ArSpawnEvent(EntityUid SourceEntity, EntityUid ViewerEntity);

/// <summary>
/// Raised on an entity to draw it on an augmented reality overlay.
/// </summary>
/// <param name="handleWorld">world handle used to draw</param>
[ByRefEvent]
public record struct ArDrawEvent(DrawingHandleWorld HandleWorld);
