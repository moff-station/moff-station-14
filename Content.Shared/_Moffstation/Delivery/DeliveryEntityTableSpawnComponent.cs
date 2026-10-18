using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Delivery;

namespace Content.Shared._Moffstation.Delivery;

/// <summary>
/// Component given to deliveries.
/// Applies a duration before which the delivery must be delivered.
/// If successful, adds a small multiplier, otherwise removes a small multiplier.
/// </summary>
[RegisterComponent]
[Access(typeof(DeliveryModifierSystem))]
public sealed partial class DeliveryEntityTableSpawnComponent : Component
{
    /// <summary>
    /// The entities spawned once the mail is triggered.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector TableSpawn;

    /// <summary>
    /// Should this delivery trigger its spawn when the delivery is broken?
    /// Requires to be fragile as well.
    /// </summary>
    [DataField]
    public bool PrimeOnBreakage = true;

    /// <summary>
    /// Should this delivery trigger its spawn when the delivery expires?
    /// Requires to be priority as well.
    /// </summary>
    [DataField]
    public bool PrimeOnExpire = true;

    /// <summary>
    /// Should the delivery delete itself after triggering its spawn?
    /// </summary>
    [DataField]
    public bool DeleteAfterSpawn = true;
}
