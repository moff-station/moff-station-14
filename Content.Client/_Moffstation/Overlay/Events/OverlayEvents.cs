using Robust.Client.Graphics;

namespace Content.Client._Moffstation.Overlay.Events;

/// <summary>
/// A function which allow to draw an entity inside an overlay
/// </summary>
public delegate void OverlayDrawingFunction(EntityUid entity, in OverlayDrawArgs args);

/// <summary>
/// Raised on an entity to draw it inside an overlay.
/// </summary>
/// <param name="Functions">functions that will be used to draw the entity</param>
/// <remarks>
/// This exist because OverlayDrawArgs cannot be passed inside a directed event (readonly ref struct),
/// otherwise it would have been preferable to make the handler perform the drawing itself.
/// </remarks>
[ByRefEvent]
public record struct GetOverlayDrawingFunctionsEvent(List<OverlayDrawingFunction> Functions);
