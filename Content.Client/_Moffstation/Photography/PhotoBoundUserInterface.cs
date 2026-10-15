using Content.Shared._Moffstation.Photography;
using Robust.Client.UserInterface;
using Robust.Shared.Graphics;

namespace Content.Client._Moffstation.Photography;

/// <summary>
/// Shows a Map using the Snapshot recorded on a photograph
/// </summary>
public sealed partial class PhotoBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private PhotoViewerWindow? _window;

    [ViewVariables]
    private EntityUid? _sceneMap;

    [ViewVariables]
    public PhotoSnapshot? Snapshot { get; private set; }

    private bool _disposed;

    public bool Disposed => _disposed;

    protected override void Open()
    {
        base.Open();

        var viewer = EntMan.System<PhotoViewerSystem>();
        if (viewer.TryGetKnownSnapshot(Owner, out var known))
        {
            SetSnapshot(known);
            return;
        }

        RequestSnapshot();
        viewer.AwaitSnapshot(this);
    }

    public void RequestSnapshot()
    {
        SendMessage(new PhotoSnapshotRequestMessage());
    }

    public void SetSnapshot(PhotoSnapshot snapshot)
    {
        if (Snapshot != null || _disposed)
            return;

        Snapshot = snapshot;

        EntMan.System<PhotoViewerSystem>().QueueBuild(this, snapshot);
    }

    public void ShowScene(EntityUid map, IEye eye, Vector2i viewportSize)
    {
        if (_disposed)
        {
            EntMan.System<PhotoViewerSystem>().Destroy(map);
            return;
        }

        _sceneMap = map;

        _window = this.CreateWindow<PhotoViewerWindow>();
        _window.SetScene(eye, viewportSize);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _disposed = true;
        if (_sceneMap is { } map)
        {
            _sceneMap = null;
            EntMan.System<PhotoViewerSystem>().Destroy(map);
        }
    }
}
