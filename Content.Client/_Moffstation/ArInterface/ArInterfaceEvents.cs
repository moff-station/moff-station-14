using Robust.Client.Graphics;

namespace Content.Client._Moffstation.ArInterface;



// a function that draw the entity inside an overlay
public delegate void ArDrawingMethod(EntityUid entity, in OverlayDrawArgs args);

/// <summary>
/// Raised directed on an entity to determine how to draw it in an overlay
/// </summary>
/// <param name="Methods">methods that will be called to draw this entity</param>
[ByRefEvent]
public record struct GetArDrawingMethodsEvent(List<ArDrawingMethod> Methods);
