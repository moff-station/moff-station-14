using Content.Client._Funkystation.SuitBreach.Systems;
using Content.Client._Starfall.Particles;

namespace Content.Client._Funkystation.SuitBreach.Components;

/// <summary>
/// the particles and hiss of a leaking suit
/// </summary>
[RegisterComponent, Access(typeof(ClientSuitBreachSystem))]
public sealed partial class SuitBreachLeakVisualsComponent : Component
{
    [ViewVariables]
    public ActiveEmitter? Emitter;

    [ViewVariables]
    public EntityUid? HissStream;
}
