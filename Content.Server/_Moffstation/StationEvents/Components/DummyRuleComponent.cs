using Content.Server._Moffstation.StationEvents.Events;

namespace Content.Server._Moffstation.StationEvents.Components;

[RegisterComponent, Access(typeof(DummyRuleSystem))]
public sealed partial class DummyRuleComponent : Component;
