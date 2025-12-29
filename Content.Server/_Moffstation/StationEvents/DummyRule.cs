using Content.Server.StationEvents.Events;

namespace Content.Server._Moffstation.StationEvents;

[RegisterComponent, Access(typeof(DummyRuleSystem))]
public sealed partial class DummyRuleComponent : Component;

public sealed class DummyRuleSystem : StationEventSystem<DummyRuleComponent>;

