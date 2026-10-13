using Content.Shared.Actions;
using Content.Shared.Preferences;

namespace Content.Shared._Moffstation.Geras;

public sealed partial class MorphGeras : InstantActionEvent;

public record struct GerasVisualInitEvent(HumanoidCharacterProfile? Profile);
