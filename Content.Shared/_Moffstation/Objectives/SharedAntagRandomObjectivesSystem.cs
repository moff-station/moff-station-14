using Robust.Shared.Player;


namespace Content.Shared._Moffstation.Objectives;

public sealed class SharedAntagRandomObjectivesSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public List<EntityUid> GetObjectiveOptions(ICommonSession session)
    {
        EntityQueryEnumerator<AntagRandomObjectivesComponent>()
    }
}
