using Content.Server._Moffstation.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Moffstation.GameTicking.Rules;

public sealed class ListeningOutpostRuleSystem : GameRuleSystem<ListeningOutpostRuleComponent>
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;


    protected override void Started(EntityUid uid, ListeningOutpostRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        _mapSystem.CreateMap(out var mapId);
    }
}
