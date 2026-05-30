using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Impstation.Tourist.Components
{
    //Largely copied from FlashComponent
    [RegisterComponent, NetworkedComponent, Access(typeof(SharedTouristCameraSystem))]
    public sealed partial class TouristCameraComponent : Component
    {

        [DataField]
        public int FlashDuration { get; set; } = 3500;

        [DataField]
        public TimeSpan FlashVisualsDuration = TimeSpan.FromMilliseconds(400);

        [DataField]
        public float Range { get; set; } = 7f;

        [DataField]
        public TimeSpan AoeFlashDuration = TimeSpan.FromSeconds(2);

        [DataField]
        public float SlowTo { get; set; } = 0.5f;

        [DataField]
        public SoundSpecifier Sound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/flash.ogg")
        {
            Params = AudioParams.Default.WithVolume(1f).WithMaxDistance(3f)
        };

        [DataField]
        public float DoAfterDuration = 2f;

        public bool Flashing;

        [DataField]
        public float Probability = 1f;
    }

    /// <summary>
    /// This component is used to automatically update visuals for <see cref="TouristCameraComponent"/>.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
    [Access(typeof(SharedTouristCameraSystem))]
    public sealed partial class ActiveTouristCameraComponent : Component
    {
        /// <summary>
        /// Time at which this flash will be considered no longer active.
        /// At this time this component will be removed.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        [AutoNetworkedField, AutoPausedField]
        public TimeSpan ActiveUntil = TimeSpan.Zero;
    }

    [Serializable, NetSerializable]
    public enum TouristCameraVisuals : byte
    {
        BaseLayer,
        LightLayer,
        Flashing,
    }
}
