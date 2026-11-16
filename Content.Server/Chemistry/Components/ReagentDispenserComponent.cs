using Content.Shared.Whitelist;
using Content.Shared.Containers.ItemSlots;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry;
using Robust.Shared.Audio;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// A machine that dispenses reagents into a solution container from containers in its storage slots.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ReagentDispenserSystem))]
    public sealed partial class ReagentDispenserComponent : Component
    {
        [DataField]
        public ItemSlot BeakerSlot = new();

        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        // Moff Start - Configurable dispense amounts
        // [ViewVariables(VVAccess.ReadWrite)]
        // public ReagentDispenserDispenseAmount DispenseAmount = ReagentDispenserDispenseAmount.U10;

        /// The amounts, in units, this dispenser can be set to dispense, in UI order.
        [DataField]
        public List<int> DispenseAmounts = [1, 5, 10, 15, 20, 30, 40, 60, 120];

        /// The currently selected amount to dispense, one of <see cref="DispenseAmounts"/>.
        [DataField]
        public int DispenseAmount = 10;
        // Moff end
    }
}
