using Content.Shared.Silicons.Laws;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.JestographicSequencer;

/// <summary>
///Raised on a target when a jestographic sequencer is used on it. You've just been jested.
/// </summary>
[ByRefEvent]
public record struct GotJestedEvent(
    EntityUid UserUid,
    ProtoId<SiliconLawsetPrototype> Lawset,
    bool Handled = false);
