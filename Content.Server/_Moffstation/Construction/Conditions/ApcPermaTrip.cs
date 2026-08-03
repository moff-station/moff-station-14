using Content.Server.Power.Components;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server._Moffstation.Construction.Conditions;

[UsedImplicitly, DataDefinition]
public sealed partial class ApcPermaTrip : IGraphCondition
{
    [DataField]
    public bool IsPermaTripped { get; private set; } = true;

    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<ApcComponent>(uid, out var apc))
            return !IsPermaTripped;
        return IsPermaTripped == apc.PermaTripped;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        if (IsPermaTripped)
            args.PushMarkup(Loc.GetString("construction-examine-condition-permatripped"));
        return IsPermaTripped;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = IsPermaTripped
                ? "construction-step-condition-permatripped"
                : "construction-step-condition-not-permatripped",
        };
    }
}
