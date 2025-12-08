using System.Linq;
using Content.Shared.Paper;
using Robust.Shared.GameObjects;
using Content.Shared._Moffstation.PizzaScurret.Receipt;

namespace Content.Shared._Moffstation.PizzaScurret.Receipt
{
    /// <summary>
    ///  This system handles the receipt, specifically who needs to signed and if their signature is present.
    ///  This needs to check the signatures against the target signature which will be selected on objective initiation.
    /// </summary>
    public sealed class PizzaReceiptSystem : EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<OnSignedEvent>(OnSigned); // When, OnSignedEvent gets called. Do OnSigned.
        }

        private void OnSigned(EntityUid uid, OnSignedEvent ev)
        {
            if (TryComp<PizzaReceiptComponent>(uid, out var comp))
            {
                comp.DetectedSignature = ev.DetectedSignature; // Make the component's DetectedSignature the same as the System's.
                Dirty(uid, comp); // Update it.

                ISawmill.Info($"PizzaReceiptSystem detected signature: {ev.DetectedSignature}"); // log the information
                if (comp.DetectedSignature == comp.RequiredSignature)
                {
                    ISawmill.Info($"Signature is a match");
                } else
                {
                    ISawmill.Info($"Signature is NOT a match");
                }
            }
        }
    }
}

public sealed class OnSignedEvent : EntityEventArgs
{
    public string DetectedSignature;
    public OnSignedEvent(string signature)
    {
        DetectedSignature = signature;
    }
}
