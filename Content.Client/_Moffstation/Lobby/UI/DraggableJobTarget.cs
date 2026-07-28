using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Moffstation.Lobby.UI;

/// <summary>
/// A drop target for <see cref="DraggableJobIcon"/>, representing one job priority. Handles hover
/// feedback, drops, and keeping the icons it holds sorted. Ported from upstream PR #36493.
/// </summary>
public sealed class DraggableJobTarget : Control
{
    private static readonly List<JobPrototype> OrderedJobsInternal = new();

    /// <summary>The sort order icons are kept in, shared by every target.</summary>
    public static ImmutableList<JobPrototype> OrderedJobs => OrderedJobsInternal.ToImmutableList();

    private readonly BoxContainer _mainBox;

    /// <summary>Shown while an icon is dragged over this target.</summary>
    private readonly PanelContainer _backgroundPanel;

    /// <summary>A <see cref="GridContainer"/>, or a <see cref="BoxContainer"/> when high priority.</summary>
    private Container? _jobIconContainer;

    /// <summary>Where the occupant goes when a new icon is dropped on the single high slot.</summary>
    private DraggableJobTarget? _fallbackTarget;

    public JobPriority Priority { get; set; }

    private bool IsHighPriority => Priority == JobPriority.High;

    public DraggableJobTarget()
    {
        _backgroundPanel = new PanelContainer
        {
            Visible = false,
            PanelOverride = new StyleBoxFlat { BackgroundColor = StyleNano.NanoGold },
        };
        AddChild(_backgroundPanel);

        _mainBox = new BoxContainer
        {
            Margin = new Thickness(4, 0),
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        AddChild(_mainBox);
    }

    /// <summary>
    /// Sets where this target bumps its occupant to. Only meaningful on the high priority target.
    /// </summary>
    public void SetFallbackTarget(DraggableJobTarget target)
    {
        if (!IsHighPriority)
            throw new InvalidOperationException("Only the high priority job target can have a fallback set");

        if (target.IsHighPriority)
            throw new InvalidOperationException("The fallback target must not also be high priority");

        _fallbackTarget = target;
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        // Built here rather than in the constructor because Priority is set by XAML afterwards.
        _mainBox.RemoveAllChildren();

        _mainBox.AddChild(new Label
        {
            Text = Loc.GetString($"humanoid-profile-editor-job-priority-{Priority.ToString().ToLower()}-button"),
            HorizontalAlignment = HAlignment.Center,
            StyleClasses = { "LabelBig" },
            Margin = new Thickness(0, 6),
        });

        // All four containers hang from the top so their icons line up under the headers.
        _jobIconContainer = IsHighPriority
            ? new BoxContainer
            {
                Name = "HighBox",
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top,
                // The width of one high priority icon, so the box does not resize when emptied.
                MinWidth = 64,
            }
            : new GridContainer
            {
                Columns = 5,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Top,
                // Tighter than the default 4 so the columns fit the lobby panel's width.
                HSeparationOverride = 2,
                VSeparationOverride = 2,
            };

        _mainBox.AddChild(_jobIconContainer);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        _mainBox.RemoveAllChildren();
        _jobIconContainer = null;
    }

    public void ClearIcons()
    {
        _jobIconContainer?.DisposeAllChildren();
    }

    public void RegisterJobIcon(DraggableJobIcon icon)
    {
        icon.OnMouseMove += pos => HandleMouseMove(pos, icon);
        icon.OnMouseUp += pos => HandleMouseUp(pos, icon);
    }

    /// <summary>
    /// Reparents <paramref name="icon"/> into this target. Pass <paramref name="preOrdered"/> when
    /// filling an empty target in order, to skip the insert search.
    /// </summary>
    public void AddJobIcon(DraggableJobIcon icon, bool preOrdered = false)
    {
        if (IsHighPriority && _jobIconContainer?.ChildCount > 0)
        {
            if (_fallbackTarget is null)
                return;

            if (_jobIconContainer.Children.First() is not DraggableJobIcon toBump)
                return;

            _fallbackTarget.AddJobIcon(toBump);
        }

        icon.SetScale(Priority);

        var insertIndex = preOrdered ? -1 : FindInsertLocation(icon);

        icon.Orphan();
        _jobIconContainer?.AddChild(icon);

        if (insertIndex >= 0)
            icon.SetPositionInParent(insertIndex);
    }

    private int FindInsertLocation(DraggableJobIcon icon)
    {
        if (IsHighPriority)
            return -1;

        var thisIndex = OrderedJobs.IndexOf(icon.JobProto);

        var insertAt = _jobIconContainer?.Children.Cast<DraggableJobIcon>()
            .ToImmutableList()
            .FindIndex(curIcon => OrderedJobs.IndexOf(curIcon.JobProto) > thisIndex);

        return insertAt ?? -1;
    }

    private void HandleMouseUp(Vector2 pos, DraggableJobIcon icon)
    {
        if (!icon.Dragging || !GlobalRect.Contains(pos))
            return;

        AddJobIcon(icon);
        _backgroundPanel.Visible = false;
    }

    private void HandleMouseMove(Vector2 pos, DraggableJobIcon icon)
    {
        var contained = GlobalRect.Contains(pos);

        _backgroundPanel.Visible = contained;

        if (contained)
            icon.SetScale(Priority);
    }

    /// <summary>Rebuilds the shared sort order. Call when job or department prototypes change.</summary>
    public static void UpdatedOrderedJobs(IPrototypeManager protoMan)
    {
        OrderedJobsInternal.Clear();

        var departments = protoMan.EnumeratePrototypes<DepartmentPrototype>().ToList();
        departments.Sort(DepartmentUIComparer.Instance);

        foreach (var department in departments)
        {
            var jobs = department.Roles.Select(protoMan.Index).Where(role => role.SetPreference).ToList();
            jobs.Sort(JobUIComparer.Instance);

            foreach (var job in jobs)
            {
                if (!OrderedJobsInternal.Contains(job))
                    OrderedJobsInternal.Add(job);
            }
        }
    }

    public IEnumerable<JobPrototype> GetContainedJobs()
    {
        if (_jobIconContainer is null)
            return [];

        return _jobIconContainer.Children.Cast<DraggableJobIcon>().Select(icon => icon.JobProto);
    }

    public int ContainedJobCount()
    {
        return _jobIconContainer?.ChildCount ?? 0;
    }

    public void SetColumns(int columns)
    {
        // Clamping to the child count keeps the icons centred, and GridContainer throws on zero.
        if (_jobIconContainer is GridContainer grid)
            grid.Columns = grid.ChildCount == 0 ? 1 : Math.Min(columns, grid.ChildCount);
    }
}
