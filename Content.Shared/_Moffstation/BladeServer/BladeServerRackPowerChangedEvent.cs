namespace Content.Shared._Moffstation.BladeServer;

// Raised on a BladeServerRackComponent right after SharedBladeServerSystem handles the rack's own
// PowerChangedEvent, so other systems that care about a rack's power (like Chitter, whose blade
// servers draw through the rack rather than having their own ApcPowerReceiver) can react without
// fighting over the single by-ref subscription slot PowerChangedEvent only allows per component.
[ByRefEvent]
public readonly record struct BladeServerRackPowerChangedEvent;
