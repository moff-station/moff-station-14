using Content.Server._MACRO.Speech.Components;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Speech.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._MACRO.Speech.EntitySystems;

// hi, this is a copy of NoContractionsAccentSystem, split to retain function of accentless for non thaven using the trait
public sealed partial class ThavenAccentComponentAccentSystem : EntitySystem
{
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    private static readonly ProtoId<ReplacementAccentPrototype> AccentDelegate = "nocontractions";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThavenAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(Entity<ThavenAccentComponent> entity, ref AccentGetEvent args)
    {
        args.Message = _replacement.ApplyReplacements(args.Message, AccentDelegate);
    }
}
