using System.Linq;
using System.Numerics;
using Content.Client._Moffstation.Preferences;
using Content.Client.Lobby.UI.ProfileEditorControls;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using MoffUI = Content.Client._Moffstation.Lobby.UI;

// Partial of the upstream LobbyCharacterPreviewPanel, so it must sit in the upstream namespace.
namespace Content.Client.Lobby.UI;

public sealed partial class LobbyCharacterPreviewPanel
{
    [Dependency] private readonly IClientPreferencesManager _moffPreferences = default!;
    [Dependency] private readonly IPrototypeManager _moffPrototypeManager = default!;
    [Dependency] private readonly JobRequirementsManager _moffRequirements = default!;
    [Dependency] private readonly IUserInterfaceManager _moffUiManager = default!;
    [Dependency] private readonly MoffCharacterSelectionManager _moffSelection = default!;

    /// <summary>Suppresses the rebuild triggered by our own save, which would run mid-callback.</summary>
    private bool _moffApplyingPriorities;

    /// <summary>The four drop targets are named after their priority, so look them up by it.</summary>
    private MoffUI.DraggableJobTarget GetMoffTarget(JobPriority priority)
    {
        return FindControl<MoffUI.DraggableJobTarget>($"{priority}Box");
    }

    private void InitializeMoffJobGrid()
    {
        _moffPrototypeManager.PrototypesReloaded += OnMoffPrototypesReloaded;
        _moffSelection.OnStateChanged += RefreshMoffJobGrid;

        MoffUI.DraggableJobTarget.UpdatedOrderedJobs(_moffPrototypeManager);

        // Dropping a second job on the single high slot has to bump the occupant somewhere.
        GetMoffTarget(JobPriority.High).SetFallbackTarget(GetMoffTarget(JobPriority.Medium));
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

            var icon = new MoffUI.DraggableJobIcon(job, () => PriorityLock.Pressed, _ => CreateMoffJobTooltip(job));

            // A job no character will take can never be assigned, however it is prioritised.
            if (GetMoffProfilesForJob(job.ID).Count == 0)
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
        const int totalColumns = 15;

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
            if (profile is HumanoidCharacterProfile humanoid && humanoid.JobPriorities.ContainsKey(job))
                result[slot] = humanoid;
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

    /// <summary>
    /// Each <see cref="MoffUI.DraggableJobTarget"/> rebuilds itself empty when it enters the tree,
    /// so the icons have to be put back afterwards.
    /// </summary>
    protected override void EnteredTree()
    {
        base.EnteredTree();

        RefreshMoffJobGrid();
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
