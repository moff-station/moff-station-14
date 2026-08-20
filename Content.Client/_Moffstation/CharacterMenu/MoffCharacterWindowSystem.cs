using Content.Shared._Moffstation.CharacterMenu;
using Content.Shared._Moffstation.Objectives;
using Content.Shared._Starlight.CollectiveMind;
using Content.Shared.DetailExaminable;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Moffstation.CharacterMenu;

/// <summary>
/// Joe Biden please help me open this window
/// </summary>
public sealed partial class MoffCharacterWindowSystem : EntitySystem
{
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<DetailExaminableComponent> _detailExaminableQuery;
    [Dependency] private EntityQuery<HumanoidProfileComponent> _humanoidProfileQuery;
    [Dependency] private EntityQuery<MindComponent> _mindQuery;
    [Dependency] private EntityQuery<MindContainerComponent> _mindContainerQuery;
    [Dependency] private EntityQuery<PotentialObjectivesComponent> _potentialObjectivesQuery;

    [SubscribeNetworkEvent]
    private void OnRoleTypeChanged(MindRoleTypeChangedEvent ev, EntitySessionEventArgs _)
    {
        _ui.GetUIController<CharacterUIController>().UpdateRoleType();
    }

    [SubscribeNetworkEvent]
    private void OnOpenCharacterMenu(OpenCharacterMenuEvent ev)
    {
        _ui.GetUIController<CharacterUIController>().OpenWindow();
    }

    public CharacterJobInfo? GetJobInfo(ProtoId<JobPrototype>? jobId)
    {
        if (!ProtoMan.Resolve(jobId, out var job))
            return null;

        return new CharacterJobInfo(job.Name, _sprite.Frame0(ProtoMan.Index(job.Icon).Icon));
    }

    public CharacterProfileInfo? GetProfileInfo(EntityUid entity)
    {
        if (!_humanoidProfileQuery.TryComp(entity, out var profile))
            return null;

        return new CharacterProfileInfo(profile.Gender, profile.Age, ProtoMan.Index(profile.Species).Name);
    }

    public string? GetDescription(EntityUid entity)
    {
        return _detailExaminableQuery.TryComp(entity, out var description) ? description.Content : null;
    }

    public CharacterRoleTypeInfo? GetRoleType(EntityUid? entity)
    {
        if (!_mindQuery.TryComp(GetMind(entity), out var mind) ||
            !ProtoMan.Resolve(mind.RoleType, out var roleType))
            return null;

        if (mind.Subtype is { } subtype)
            return new CharacterRoleTypeInfo(subtype, mind.SubtypeColor ?? roleType.Color);

        return new CharacterRoleTypeInfo(roleType.Name, roleType.Color);
    }

    public bool CanPickObjectives(EntityUid? entity)
    {
        return _potentialObjectivesQuery.HasComp(GetMind(entity));
    }

    public List<CollectiveMindInfo> GetCollectiveMinds(
        Dictionary<ProtoId<CollectiveMindPrototype>, CollectiveMindMemberData>? minds)
    {
        var infos = new List<CollectiveMindInfo>();
        if (minds == null)
            return infos;

        foreach (var (mindId, member) in minds)
        {
            if (!ProtoMan.Resolve(mindId, out var mind))
                continue;

            infos.Add(new CollectiveMindInfo(mind.Name, mind.KeyCode, mind.Color, member.MindId));
        }

        return infos;
    }

    private EntityUid? GetMind(EntityUid? entity)
    {
        return _mindContainerQuery.TryComp(entity, out var container) ? container.Mind : null;
    }

    public readonly record struct CharacterJobInfo(LocId Name, Texture Icon);

    public readonly record struct CharacterProfileInfo(Gender Gender, int Age, LocId Species);

    public readonly record struct CharacterRoleTypeInfo(LocId Name, Color Color);

    public readonly record struct CollectiveMindInfo(LocId Name, char KeyCode, Color Color, int MindId);
}
