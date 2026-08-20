using Content.Shared.Preferences;

namespace Content.Client.Lobby.UI;

/// <summary>
/// A character only records <i>whether</i> it will take a job. The priority applied to that job is
/// player-global and lives in the job priority editor, so the four-way selector collapses to yes/no.
/// </summary>
public sealed partial class HumanoidProfileEditor
{
    // RadioOptions ids are auto-incremented, not the values passed in, so these must stay
    // index-aligned. Upstream's four entries only lined up with JobPriority by coincidence.
    private const int MoffJobPreferenceNo = 0;
    private const int MoffJobPreferenceYes = 1;

    private static readonly (string, int)[] MoffJobPreferenceItems =
    [
        ("humanoid-profile-editor-job-preference-no-button-moffstation", MoffJobPreferenceNo),
        ("humanoid-profile-editor-job-preference-yes-button-moffstation", MoffJobPreferenceYes),
    ];

    /// <summary>Maps a selector id back to the only two priorities a character can express.</summary>
    private static JobPriority MoffToJobPriority(int selected)
    {
        return selected == MoffJobPreferenceNo ? JobPriority.Never : JobPriority.Medium;
    }

    /// <summary>Any legacy Low/High on a character collapses onto "yes".</summary>
    private static int MoffFromJobPriority(JobPriority priority)
    {
        return priority == JobPriority.Never ? MoffJobPreferenceNo : MoffJobPreferenceYes;
    }

    /// <summary>
    /// Superseded by the player-global priority editor, which has a Passenger priority of its own.
    /// Kept visible-false rather than removed so the setting and its DB column still load.
    /// </summary>
    private void HideMoffPreferenceUnavailable()
    {
        PreferenceUnavailableButton.Visible = false;
    }
}
