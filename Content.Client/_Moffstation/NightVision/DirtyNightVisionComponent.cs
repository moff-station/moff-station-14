namespace Content.Client._Moffstation.NightVision;

/// This marker component exists only to indicate that its owning entity needs to have NightVision applied. Does nothing
/// if on an entity without <see cref="Content.Shared._Moffstation.NightVision"/>
[RegisterComponent]
public sealed partial class DirtyNightVisionComponent : Component;
