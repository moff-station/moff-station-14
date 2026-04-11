using Robust.Shared.Serialization;

//this needs to exist here to use enum.DamageStateVisualLayers.Base as the sprite layer for a VisualOrganComponent
namespace Content.Shared._Moffstation.DamageState
{
    [Serializable, NetSerializable]
    public enum DamageStateVisualLayers : byte
    {
        Base,
        BaseUnshaded
    }
}
