using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cargo.Prototypes;

/// <summary>
/// This is a prototype for a single account that stores money on StationBankAccountComponent
/// </summary>
[Prototype]
public sealed partial class CargoAccountPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Full IC name of the account.
    /// </summary>
    [DataField]
    public LocId Name;

    /// <summary>
    /// A shortened code used to refer to the account in UIs
    /// </summary>
    [DataField]
    public LocId Code;

    /// <summary>
    /// Color corresponding to the account.
    /// </summary>
    [DataField]
    public Color Color;

    /// <summary>
    /// Channel used for announcing transactions.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel;

    /// <summary>
    ///  Moffstation - String for secret accounts (for use by antags), disconnected from the rest of the accounts
    /// </summary>
    [DataField("secretAccount")]
    public string? SecretAccount;
}
