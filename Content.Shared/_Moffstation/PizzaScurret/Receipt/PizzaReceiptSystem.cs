using System.Linq;

namespace Content.Shared._Moffstation.PizzaScurret.Receipt;

/// <summary>
///  This system handles the receipt, specifically who needs to signed and if their signature is present.
///  This needs to check the signatures against the target signature which will be selected on objective initiation.
/// </summary>
public sealed class PizzaReceiptSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
    }

/*
    private void CheckReceipt(Entity<PizzaReceiptComponent> ent, EntityUid customer)
    {
        foreach ( var stamp in paper.StampedBy) // Do this for every signature on the paper.
        {
            var signature = stamp.StampedName; // Get the signature.
            Logger.Info($"Signature present: {signature}"); // Log it for debugging.

            if (signature == targetSignature) // If the signature matches the target signature
            {
                Logger.Info($"Signature matched: {signature}"); // Log the match for debugging.
                return true; // Return true if a match is found.
            }
            else
            false;
        }
    } */
}

