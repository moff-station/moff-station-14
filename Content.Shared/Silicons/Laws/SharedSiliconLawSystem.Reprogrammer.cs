using Content.Shared._Moffstation.Silicons.LawReprogrammer;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared.Silicons.Laws;

// Moffstation - Cyborg law alteration

public abstract partial class SharedSiliconLawSystem
{
    public void InitializeReprogrammer()
    {
        SubscribeLocalEvent<BorgChassisComponent, GotReprogrammedEvent>(OnChassisReprogrammed);
        SubscribeLocalEvent<BorgBrainComponent, GotReprogrammedEvent>(OnBrainReprogrammed);
    }

    private void OnChassisReprogrammed(Entity<BorgChassisComponent> ent, GotReprogrammedEvent ev)
    {

    }

    private void OnBrainReprogrammed(Entity<BorgBrainComponent> ent, GotReprogrammedEvent ev)
    {

    }
}

// Moffstation - End
