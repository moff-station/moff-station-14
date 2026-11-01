namespace Content.Shared._Moffstation.JestographicSequencer;

[ByRefEvent]
public record struct JestographicCameraWireCutEvent(EntityUid UserUid, bool Handled = false);