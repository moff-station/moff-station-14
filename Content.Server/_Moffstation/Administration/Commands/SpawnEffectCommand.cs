using Content.Server.Sandbox;

namespace Content.Server._Moffstation.Administration.Commands;

public sealed class SpawnEffectCommand : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntParentChangedMessage>(OnParentChanged);
    }

    private void OnParentChanged(ref EntParentChangedMessage ev)
    {
        // 1. Check if we are currently inside a Placement action
        if (!SandboxSystem.IsPlacementProcessing)
            return;

        // 2. Only trigger on the initial spawn (moving from null to a map)
        if (ev.OldParent != EntityUid.Invalid)
            return;

        // 3. Reset the flag immediately so we don't catch subsequent unrelated spawns
        SandboxSystem.IsPlacementProcessing = false;

        // 4. Run your effect
        var coords = Transform(ev.Entity).Coordinates;
        Spawn("EffectFlashBluespace", coords);
    }
}
