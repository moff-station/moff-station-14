namespace Content.Server._Moffstation.Antag;

/// <summary>
/// This is used for marking that an antag shouldn't influence weighted selection
/// In other words, if you don't get selected for this role, you don't get an antag weight
/// </summary>
[RegisterComponent]
public sealed partial class MoffUnweightedAntagComponent : Component;
