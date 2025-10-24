using Content.Shared.Mobs.Components;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;

namespace Content.Shared._Moffstation.Paper.Systems;

public sealed class StampSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StampComponent, MeleeHitEvent>(OnAttack);
    }

    private void OnAttack(Entity<StampComponent> ent, ref MeleeHitEvent args)
    {
        foreach (var hitEnt in args.HitEntities)
        {
            // If it aint a mob we dont care
            if (!HasComp<MobStateComponent>(hitEnt))
                continue;

            var stampPaperOtherMessage = Loc.GetString("paper-component-action-stamp-paper-other",
                ("user", args.User),
                ("target", hitEnt),
                ("stamp", args.Weapon));

            _popupSystem.PopupEntity(stampPaperOtherMessage, hitEnt, Filter.PvsExcept(args.User, entityManager: EntityManager), true);

            var stampPaperSelfMessage = Loc.GetString("paper-component-action-stamp-paper-self",
                ("target", hitEnt),
                ("stamp", args.Weapon));
            _popupSystem.PopupClient(stampPaperSelfMessage, hitEnt, args.User);
        }
    }
}
