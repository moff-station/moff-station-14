using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Starlight.CollectiveMind;

public sealed partial class CollectiveMindUpdateSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private readonly Dictionary<CollectiveMindPrototype, int> _globalMindIdTracker = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CollectiveMindComponent, ComponentStartup>(OnCollectiveMindInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnCollectiveMindInit(Entity<CollectiveMindComponent> entity, ref ComponentStartup args)
    {
        UpdateCollectiveMind(entity.AsNullable());
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _globalMindIdTracker.Clear();
    }

    public void ForceCloneFrom(EntityUid source, EntityUid target)
    {
        if (!TryComp<CollectiveMindComponent>(source, out var component) ||
            !TryComp<CollectiveMindComponent>(target, out var targetComponent))
            return;

        targetComponent.Minds.Clear();

        foreach (var (proto, mind) in component.Minds)
        {
            targetComponent.Minds.Add(proto, mind);
        }

        UpdateCollectiveMind((target, targetComponent)); //capture any we missed
    }

    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public void UpdateCollectiveMind(Entity<CollectiveMindComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return;

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<CollectiveMindPrototype>())
        {
            var any = false;
            foreach (var requirement in prototype.Requirements)
            {
                if (_whitelist.IsWhitelistPass(requirement, entity))
                {
                    any = true;
                    break;
                }
            }

            if (!any)
            {
                entity.Comp.Minds.Remove(prototype);
                continue;
            }

            var id = _globalMindIdTracker.GetOrNew(prototype);
            entity.Comp.Minds.TryAdd(
                prototype,
                new CollectiveMindMemberData { MindId = id }
            );
            _globalMindIdTracker[prototype] = id + 1;
        }
    }
}
