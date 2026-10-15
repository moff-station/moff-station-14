using Content.Shared._Moffstation.Photography;

namespace Content.Server._Moffstation.Photography;

/// <summary>
/// The scene a photograph shows. Saved Serverside and send when requested
/// </summary>
[RegisterComponent]
public sealed partial class PhotoSnapshotComponent : Component
{
    [ViewVariables]
    public PhotoSnapshot? Snapshot;
}
