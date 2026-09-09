using Content.Shared.Flash.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._Moffstation.Overlays;

/// This marker component causes entities with both <see cref="Content.Shared.Overlays.NightVisionComponent"/> and
/// <see cref="FlashImmunityComponent">flash immunity</see> to have its night vision disabled.
[RegisterComponent, NetworkedComponent]
public sealed partial class NightVisionDisabledByFlashImmunityComponent : Component;
