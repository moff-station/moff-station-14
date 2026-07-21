namespace Content.Server._Moffstation.Antag;

/// <summary>
/// If you don't get selected for a rule with this component, you don't get an antag weight.
/// However, because of how antag selection is set up right now, your weight still influences your odds of being selected.
/// Having it do otherwise would be very complex and I don't feel like setting it up right now! its also fairly inconsequential atm
/// </summary>
// TODO: Make rules with this component not use weights in selection
[RegisterComponent]
public sealed partial class MoffUnweightedAntagComponent : Component;
