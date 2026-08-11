using Content.Server._Moffstation.Shuttles.Systems;

namespace Content.Server._Moffstation.Shuttles.Components;

/// <summary>
/// Marks a shuttle grid as FTLing out of the sector for good. See <see cref="ExitSectorSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(ExitSectorSystem))]
public sealed partial class ExitingSectorComponent : Component
{
    /// <summary>
    /// The throwaway map this shuttle is headed to, deleted along with the shuttle once it arrives.
    /// </summary>
    [DataField]
    public EntityUid? ExitMap;
}
