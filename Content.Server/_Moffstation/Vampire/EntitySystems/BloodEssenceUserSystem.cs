using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Body.Components;
using Content.Shared._Moffstation.Vampire.Components;
using Content.Shared._Moffstation.Vampire.EntitySystems;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Moffstation.Vampire.EntitySystems;

/// <summary>
/// An adapter for handing bloodstream and stomach interactions for specifically vampires but could be used by other
/// creatures which utilize BloodEssence or just drink blood.
/// </summary>
/// <remarks>
/// Eventually a lot of this functionality could be adapted to instead use the
/// <see cref="Content.Server.Chemistry.EntitySystems.InjectorSystem.DrawFromBlood"/> method, however that will
/// require some disparate changes to that system which will be merge-conflict bait for the time being.
/// If this ever gets upstreamed though it would be best to do it that way.
/// </remarks>
public sealed partial class BloodEssenceUserSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly BloodEssenceSystem _bloodEssenceSystem = default!;

    /// <summary>
    /// Extracts blood from the target creature and places it in the user's stomach.
    /// This also handles giving the target the BloodEssenceComponent and interacts with it to
    /// pull essence from the target and put it in the user's BloodEssence pool
    /// </summary>
    /// <returns>The amount of blood essence extracted.</returns>
    public float TryExtractBlood(Entity<BloodEssenceUserComponent, BodyComponent> entity, float transferAmount, Entity<BloodstreamComponent> target)
    {
        if (transferAmount <= 0.0f) // can't take 0 blood from the target
            return 0.0f;

	    entity.Deconstruct(out var uid, out var bloodEssenceUser, out var body);
	    var targetBloodstream = target.Comp;

	    if (!_body.TryGetBodyOrganEntityComps<StomachComponent>((uid, body), out var stomachs))
	        return 0.0f;

        var firstStomach = stomachs.FirstOrNull(stomach => _stomach.MaxTransferableSolution(stomach, transferAmount) > 0.0f);

        // All stomachs are full or null somehow
        if (firstStomach == null)
            return 0.0f;

        var transferableAmount = _stomach.MaxTransferableSolution(firstStomach.Value, transferAmount);

        var tempSolution = new Solution
        {
            MaxVolume = transferableAmount
        };

        if (_solutionContainerSystem.ResolveSolution(target.Owner, targetBloodstream.ChemicalSolutionName, ref targetBloodstream.ChemicalSolution, out var targetChemSolution))
        {
            // make a fraction of what we pull come from the chem solution
            // Technically this does allow someone to drink blood in order to then have that blood be taken and
            // give essence but I don't care too much about that possible issue.
            tempSolution.AddSolution(targetChemSolution.SplitSolution(transferableAmount * 0.15f), _proto);
            transferableAmount -= (float) tempSolution.Volume;
            _solutionContainerSystem.UpdateChemicals(targetBloodstream.ChemicalSolution.Value);
        }

        if (_solutionContainerSystem.ResolveSolution(target.Owner, targetBloodstream.BloodSolutionName, ref targetBloodstream.BloodSolution, out var targetBloodSolution))
        {
            tempSolution.AddSolution(targetBloodSolution.SplitSolution(transferableAmount), _proto);
            _solutionContainerSystem.UpdateChemicals(targetBloodstream.BloodSolution.Value);
        }

        var essenceCollected = 0.0f;

        if (HasComp<MobStateComponent>(target)
            && HasComp<HumanoidAppearanceComponent>(target)
            && !HasComp<BloodEssenceUserComponent>(target))
        {
            // check how much blood is in our temporary solution and subtract it from their BloodEssence component.
            var bloodEssence = EnsureComp<BloodEssenceComponent>(target);

            foreach (var reagentProto in bloodEssenceUser.BloodWhitelist)
            {
                if (!TryGetReagentQuantityByProto(tempSolution, reagentProto, out var volume))
                    continue;
                essenceCollected += _bloodEssenceSystem.Withdraw((target, bloodEssence), volume);
            }
        }

        if (!bloodEssenceUser.FedFrom.TryAdd(target,essenceCollected))
            bloodEssenceUser.FedFrom[target] += essenceCollected;
        bloodEssenceUser.BloodEssenceTotal += essenceCollected;

        _stomach.TryTransferSolution(firstStomach.Value, tempSolution);
        Dirty<StomachComponent>(firstStomach.Value);

        return essenceCollected;
    }

    /// <summary>
    /// Pretty much a copy of <see cref="Content.Shared.Chemistry.Components.Solution.TryGetReagentQuantity"/>
    /// but compares by prototype instead.
    /// </summary>
    /// <remarks>
    /// todo: Move this to Solutions.
    /// </remarks>
    private static bool TryGetReagentQuantityByProto(Solution solution, ProtoId<ReagentPrototype> proto, out float volume)
    {
        foreach (var reagentId in solution.Contents)
        {
            if (reagentId.Reagent.Prototype != proto)
                continue;
            volume = (float) reagentId.Quantity;
            return true;
        }

        volume = 0.0f;
        return false;
    }
}
