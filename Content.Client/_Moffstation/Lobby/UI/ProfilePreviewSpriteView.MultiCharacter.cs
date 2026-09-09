using Content.Client._Moffstation.Preferences;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView
{
    private static ProtoId<JobPrototype>? GetMoffPreferredJob(HumanoidCharacterProfile profile)
    {
        return IoCManager.Resolve<MoffCharacterSelectionManager>().GetPreferredJob(profile);
    }
}
