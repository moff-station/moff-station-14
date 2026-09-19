namespace Content.Server._Moffstation.Anomaly.AnomalyCreature.Components;

[RegisterComponent]
public sealed partial class LightExtinguisherComponent : Component
{
    /// <summary>
    /// The radius around the entity to disable light sources.
    /// </summary>
    [DataField]
    public float Radius = 10.0f;
}
