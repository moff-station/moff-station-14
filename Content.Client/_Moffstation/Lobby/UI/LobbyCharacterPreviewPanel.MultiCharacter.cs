using System.Linq;
using System.Numerics;
using Content.Client._Moffstation.Preferences;
using Content.Client.Lobby.UI.ProfileEditorControls;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.Stylesheets;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using MoffUI = Content.Client._Moffstation.Lobby.UI;

namespace Content.Client.Lobby.UI;

public sealed partial class LobbyCharacterPreviewPanel
{
    [Dependency] private IClientPreferencesManager _moffPreferences = default!;
    [Dependency] private IPrototypeManager _moffPrototypeManager = default!;
    [Dependency] private JobRequirementsManager _moffRequirements = default!;
    [Dependency] private IUserInterfaceManager _moffUiManager = default!;
    [Dependency] private MoffCharacterSelectionManager _moffSelection = default!;

    /// <summary>Suppresses the rebuild triggered by our own save, which would run mid-callback.</summary>
    private bool _moffApplyingPriorities;

    /// <summary>Built in code rather than XAML so the upstream .xaml file needs no Moff edit.</summary>
    private readonly Dictionary<JobPriority, MoffUI.DraggableJobTarget> _moffTargets = new();

    private CheckBox _moffPriorityLock = default!;

    private MoffUI.DraggableJobTarget GetMoffTarget(JobPriority priority)
    {
        return _moffTargets[priority];
    }

    /// <summary>A vertical gold rule, matching the ones elsewhere in the lobby.</summary>
    private static PanelContainer MakeMoffDivider()
    {
        return new PanelContainer
        {
            MinSize = new Vector2(2, 0),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = StyleNano.NanoGold,
                ContentMarginTopOverride = 2,
            },
        };
    }

    private void InitializeMoffJobGrid()
    {
        MoffUI.DraggableJobTarget.UpdatedOrderedJobs(_moffPrototypeManager);

        BuildMoffHeader();
        BuildMoffGrid();

        // Dropping a second job on the single high slot has to bump the occupant somewhere.
        GetMoffTarget(JobPriority.High).SetFallbackTarget(GetMoffTarget(JobPriority.Medium));

        // Subscribed last: both callbacks refresh the grid, which the targets must exist for.
        _moffPrototypeManager.PrototypesReloaded += OnMoffPrototypesReloaded;
        _moffSelection.OnStateChanged += RefreshMoffJobGrid;
    }

    /// <summary>Puts the priority lock next to the upstream heading, on the same row.</summary>
    private void BuildMoffHeader()
    {
        _moffPriorityLock = new CheckBox
        {
            Text = Loc.GetString("moff-lobby-lock-priorities-checkbox-label"),
            ToolTip = Loc.GetString("moff-lobby-lock-priorities-checkbox-tooltip"),
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Right,
            VerticalAlignment = VAlignment.Center,
        };

        var index = Header.GetPositionInParent();

        Header.Orphan();

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children = { Header, _moffPriorityLock },
        };

        VBox.AddChild(row);
        row.SetPositionInParent(index);
    }

    /// <summary>
    /// The job grid supersedes the single-character summary and portrait, which stay in the tree so
    /// LobbyUIController's preview refresh keeps working untouched.
    /// </summary>
    private void BuildMoffGrid()
    {
        Summary.Visible = false;
        ViewBox.Visible = false;

        var grid = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            VerticalExpand = true,
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Center,
            MinHeight = 220,
            SeparationOverride = 2,
        };

        var first = true;

        foreach (var priority in new[] { JobPriority.Never, JobPriority.Low, JobPriority.Medium, JobPriority.High })
        {
            if (!first)
                grid.AddChild(MakeMoffDivider());

            first = false;

            var target = new MoffUI.DraggableJobTarget { Priority = priority };
            _moffTargets[priority] = target;
            grid.AddChild(target);
        }

        Loaded.AddChild(grid);
        grid.SetPositionInParent(ViewBox.GetPositionInParent() + 1);
    }

    private void OnMoffPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<JobPrototype>() && !args.WasModified<DepartmentPrototype>())
            return;

        MoffUI.DraggableJobTarget.UpdatedOrderedJobs(_moffPrototypeManager);
        RefreshMoffJobGrid();
    }

    /// <summary>Rebuilds every job icon from the player's global priorities.</summary>
    public void RefreshMoffJobGrid()
    {
        if (_moffApplyingPriorities)
            return;

        foreach (var priority in Enum.GetValues<JobPriority>())
        {
            GetMoffTarget(priority).ClearIcons();
        }

        if (_moffPreferences.Preferences == null)
            return;

        var priorities = _moffSelection.State.JobPriorities;

        foreach (var job in MoffUI.DraggableJobTarget.OrderedJobs)
        {
            if (!job.SetPreference || !_moffRequirements.IsAllowed(job, null, out _))
                continue;

            var icon = new MoffUI.DraggableJobIcon(job, () => _moffPriorityLock.Pressed, _ => CreateMoffJobTooltip(job));

            // A job no character will take can never be assigned, however it is prioritised.
            if (GetMoffProfilesForJob(job.ID).All(p => !_moffSelection.IsSlotEnabled(p.Key)))
                icon.Modulate = Color.Salmon;

            foreach (var priority in Enum.GetValues<JobPriority>())
            {
                GetMoffTarget(priority).RegisterJobIcon(icon);
            }

            icon.OnPriorityChanged += OnMoffPriorityChanged;

            GetMoffTarget(priorities.GetValueOrDefault(job.ID, JobPriority.Never)).AddJobIcon(icon, preOrdered: true);
        }

        BalanceMoffColumns();
    }

    /// <summary>
    /// Splits a fixed column budget between the three multi-job buckets, preferring the layout with
    /// the fewest rows and, among those, the most even spread.
    /// </summary>
    private void BalanceMoffColumns()
    {
        // Sized so the three grids plus the high slot, rules and margins stay inside the lobby's
        // right panel (server.lobby_right_panel_width, 500 by default) at an 8px icon and scale 3.
        const int totalColumns = 12;

        // Each header label is this many columns wide on its own, so never go narrower.
        const int minNever = 3;
        const int minLow = 2;
        const int minMedium = 3;

        var counts = new[]
        {
            GetMoffTarget(JobPriority.Never).ContainedJobCount(),
            GetMoffTarget(JobPriority.Low).ContainedJobCount(),
            GetMoffTarget(JobPriority.Medium).ContainedJobCount(),
        };

        var bestHeight = int.MaxValue;
        var bestSquare = int.MaxValue;
        var best = (minNever, minLow, totalColumns - minNever - minLow);

        for (var never = minNever; never <= totalColumns - minLow - minMedium; never++)
        {
            for (var low = minLow; low <= totalColumns - never - minMedium; low++)
            {
                var medium = totalColumns - never - low;
                var columns = new[] { never, low, medium };

                // Ceiling division, so 10 icons over 5 columns is 2 rows rather than 3.
                var height = counts.Zip(columns).Select(x => (x.First - 1) / x.Second + 1).Max();

                // Minimising the sum of squares spreads the columns out evenly.
                var square = never * never + low * low + medium * medium;

                if (height > bestHeight || height == bestHeight && square >= bestSquare)
                    continue;

                bestHeight = height;
                bestSquare = square;
                best = (never, low, medium);
            }
        }

        GetMoffTarget(JobPriority.Never).SetColumns(best.Item1);
        GetMoffTarget(JobPriority.Low).SetColumns(best.Item2);
        GetMoffTarget(JobPriority.Medium).SetColumns(best.Item3);
    }

    private void OnMoffPriorityChanged()
    {
        BalanceMoffColumns();

        var result = new Dictionary<ProtoId<JobPrototype>, JobPriority>();

        foreach (var priority in Enum.GetValues<JobPriority>())
        {
            // Never is the absence of an entry.
            if (priority == JobPriority.Never)
                continue;

            foreach (var job in GetMoffTarget(priority).GetContainedJobs())
            {
                result[job.ID] = priority;
            }
        }

        // The grid already reflects this change, and rebuilding it here would dispose the icon
        // whose event we are still inside.
        _moffApplyingPriorities = true;

        try
        {
            _moffSelection.UpdateJobPriorities(result);
        }
        finally
        {
            _moffApplyingPriorities = false;
        }
    }

    /// <summary>Every character willing to take <paramref name="job"/>, keyed by slot.</summary>
    private Dictionary<int, HumanoidCharacterProfile> GetMoffProfilesForJob(ProtoId<JobPrototype> job)
    {
        var result = new Dictionary<int, HumanoidCharacterProfile>();

        if (_moffPreferences.Preferences is not { } prefs)
            return result;

        foreach (var (slot, profile) in prefs.Characters)
        {
            if (profile != null && profile.JobPriorities.ContainsKey(job))
                result[slot] = profile;
        }

        return result;
    }

    /// <summary>Lists which of the player's characters would fill this job.</summary>
    private Tooltip? CreateMoffJobTooltip(JobPrototype job)
    {
        if (_moffPreferences.Preferences == null)
            return null;

        var tooltip = new Tooltip();
        var content = tooltip.GetChild(0);
        content.RemoveAllChildren();

        var title = new Label
        {
            Text = job.LocalizedName,
            HorizontalAlignment = HAlignment.Center,
        };
        title.AddStyleClass("LabelHeading");
        content.AddChild(title);

        var grid = new GridContainer
        {
            MaxGridHeight = _moffUiManager.PopupRoot.Height * 0.99f,
            Margin = new Thickness(6),
        };
        content.AddChild(grid);

        var profiles = GetMoffProfilesForJob(job.ID);

        if (profiles.Count == 0)
        {
            grid.AddChild(new Label
            {
                Text = Loc.GetString("moff-lobby-tooltip-no-characters-for-job", ("job", job.LocalizedName)),
                Align = Label.AlignMode.Center,
            });

            return tooltip;
        }

        foreach (var (slot, profile) in profiles)
        {
            var enabled = _moffSelection.IsSlotEnabled(slot);

            var preview = new ProfilePreviewSpriteView
            {
                SetSize = new Vector2(64),
                Scale = new Vector2(2),
                HorizontalAlignment = HAlignment.Right,
            };

            if (!enabled)
                preview.Modulate = Color.Salmon;

            preview.LoadPreview(profile, job);

            var description = profile.Name;

            if (!enabled)
                description += $"\n{Loc.GetString("moff-character-disabled-label")}";

            var container = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal };

            container.AddChild(new Label
            {
                Text = description,
                Align = Label.AlignMode.Right,
                HorizontalAlignment = HAlignment.Right,
                HorizontalExpand = true,
                Margin = new Thickness(0, 0, 10, 0),
            });
            container.AddChild(preview);

            grid.AddChild(container);
        }

        return tooltip;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _moffPrototypeManager.PrototypesReloaded -= OnMoffPrototypesReloaded;
        _moffSelection.OnStateChanged -= RefreshMoffJobGrid;
    }
}
