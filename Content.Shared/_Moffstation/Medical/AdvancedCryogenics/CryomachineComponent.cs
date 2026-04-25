using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Moffstation.Medical.AdvancedCryogenics;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CryomachineComponent : Component
{
    #region slots

    /// <summary>
    /// The ID of the itemslot that holds the cryocapsule
    /// </summary>
    [DataField]
    public string CapsuleSlotId = "brain_slot";

    /// <summary>
    /// The <see cref="ItemSlot"/> that holds the cryocapsule.
    /// </summary>
    [DataField]
    public ItemSlot CapsuleSlot = new();

    /// <summary>
    /// The ID of the itemslot that holds beakers
    /// </summary>
    [DataField]
    public string BeakerSlotId = "beaker_slot";

    /// <summary>
    /// The <see cref="ItemSlot"/> that can hold beakers.
    /// </summary>
    [DataField]
    public ItemSlot BeakerSlot = new();

    #endregion

    /// <summary>
    /// Specifies the name of the atmospherics port to draw gas from.
    /// </summary>
    [DataField]
    public string PortName = "port";


    /// <summary>
    /// Time interval between two UI updates
    /// </summary>
    [DataField]
    public TimeSpan UiUpdateInterval = TimeSpan.FromSeconds(0.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUiUpdate = TimeSpan.FromSeconds(0);

    /// <summary>
    /// The sound emitted when we shock the brain.
    /// </summary>
    [DataField]
    public SoundSpecifier ShockSound = new SoundPathSpecifier("/Audio/Machines/airlock_electrify_on.ogg");

    [DataField]
    public SoundSpecifier DetachSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
}
