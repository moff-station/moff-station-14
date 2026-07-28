using System.Numerics;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Moffstation.Lobby.UI;

/// <summary>
/// A job icon that can be dragged between <see cref="DraggableJobTarget"/>s to set its priority.
/// Ported from upstream PR #36493.
/// </summary>
public sealed class DraggableJobIcon : TextureRect
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private const float DefaultScale = 3;

    /// <summary>Only one job can be high priority, so it gets drawn much larger.</summary>
    private const float DefaultHighScale = 8;

    public JobPrototype JobProto { get; }

    /// <summary>Where the icon came from, so it can snap back if nothing catches it.</summary>
    private Control? _oldParent;

    private Vector2? _oldScale;

    public bool Dragging => _oldParent is not null;

    public event Action<GUIBoundKeyEventArgs>? OnMouseDown;

    public event Action<Vector2>? OnMouseUp;

    public event Action<Vector2>? OnMouseMove;

    /// <summary>Raised once the icon has settled in a different target.</summary>
    public event Action? OnPriorityChanged;

    /// <summary>Checked before a drag starts; returning false cancels it.</summary>
    public delegate bool CheckCanDrag();

    private readonly CheckCanDrag? _canDragFunc;

    public DraggableJobIcon(
        JobPrototype jobPrototype,
        CheckCanDrag? checkDrag = null,
        TooltipSupplier? tooltipSupplier = null)
    {
        IoCManager.InjectDependencies(this);

        JobProto = jobPrototype;
        _canDragFunc = checkDrag;

        var sprite = _entManager.System<SpriteSystem>();
        var iconProto = _prototypeManager.Index(jobPrototype.Icon);

        Texture = sprite.Frame0(iconProto.Icon);
        TextureScale = new Vector2(DefaultScale);
        VerticalAlignment = VAlignment.Center;
        HorizontalAlignment = HAlignment.Center;
        MouseFilter = MouseFilterMode.Pass;

        // Suppress the tooltip while dragging, or it follows the cursor around.
        if (tooltipSupplier is not null)
            TooltipSupplier = obj => Dragging ? null : tooltipSupplier(obj);
    }

    private void StopDragging()
    {
        // Nothing caught the icon, so put it back where it came from.
        if (Parent == _uiManager.PopupRoot)
        {
            Orphan();
            _oldParent?.AddChild(this);

            if (_oldScale is not null)
                TextureScale = _oldScale.Value;
        }

        if (Parent != _oldParent)
            OnPriorityChanged?.Invoke();

        _oldParent = null;
        _oldScale = null;
    }

    private void StartDragging()
    {
        _oldParent = Parent;
        _oldScale = TextureScale;

        Orphan();
        _uiManager.PopupRoot.AddChild(this);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!Dragging)
            return;

        var mousePos = _uiManager.MousePositionScaled.Position;
        LayoutContainer.SetPosition(this, mousePos - Size / 2.0f);
        OnMouseMove?.Invoke(mousePos);
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick || _canDragFunc is null || !_canDragFunc())
            return;

        StartDragging();
        OnMouseDown?.Invoke(args);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        OnMouseUp?.Invoke(_uiManager.MousePositionScaled.Position);
        StopDragging();
    }

    public void SetScale(JobPriority priority)
    {
        TextureScale = new Vector2(priority == JobPriority.High ? DefaultHighScale : DefaultScale);
    }
}
