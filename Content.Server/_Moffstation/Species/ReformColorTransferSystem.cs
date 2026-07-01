using Content.Server._Moffstation.Geras;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Sprite;
using Robust.Shared.GameObjects;
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
        Color? color = null;
        foreach (var (_, selected) in rsc.Selected)
        {
            if (selected.Color is { } c)
            {
                color = c;
                break;
            }
        }

        if (color is not { } transferColor)
            return;

        var newAppearance = args.Profile.Appearance.WithSkinColor(transferColor);
        var newProfile = args.Profile.WithCharacterAppearance(newAppearance);

        _visualBody.ApplyProfileTo(args.Child.Owner, newProfile);
        _humanoidProfile.ApplyProfileTo(args.Child, newProfile);

        RaiseLocalEvent(args.Child.Owner, new GerasVisualInitEvent(newProfile));

        Dirty(args.Child);
    }
}
