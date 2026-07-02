using Content.Server._Moffstation.Geras;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Sprite;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using static Content.Shared.Species.ReformSystem;

namespace Content.Server._Moffstation.Species;

/// <summary>
/// When an entity with RandomSpriteComponent "Reforms" this helps transfer their random colors onto their new sprite
/// meant for geras but theoretically can be applied elsewhere
/// </summary>
public sealed partial class ReformColorTransferSystem : EntitySystem
{
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomSpriteComponent, PostReformEvent>(OnReformed);
    }

    private void OnReformed(EntityUid uid, RandomSpriteComponent rsc, PostReformEvent args)
    {
        if (rsc.Selected.Values.FirstOrNull(x => x.Color != null) is not { Color: {} transferColor })
            return;

        var newAppearance = args.Profile.Appearance.WithSkinColor(transferColor);
        var newProfile = args.Profile.WithCharacterAppearance(newAppearance);

        _visualBody.ApplyProfileTo(args.Child.Owner, newProfile);
        _humanoidProfile.ApplyProfileTo(args.Child, newProfile);

        RaiseLocalEvent(args.Child.Owner, new GerasVisualInitEvent(newProfile));

        Dirty(args.Child);
    }
}
