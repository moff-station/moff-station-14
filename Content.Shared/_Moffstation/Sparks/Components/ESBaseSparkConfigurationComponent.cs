using Content.Shared._ES.Sparks;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Sparks.Components;

/// <summary>
/// This is a base component that contains details about
/// configuring sparks in order to be reused with minimal duplication
/// </summary>
/// <remarks>Unsurprisingly, this was originally in the ES namespace, but Moff "rewrote" it enough that it gets to be moved.</remarks>
// ReSharper disable once InconsistentNaming // Retaining the name from ES where it's an abstract class.
public partial interface ESBaseSparkConfigurationComponent
{
    Config SparkConfig { get; }

    [DataRecord]
    sealed partial record Config
    {
        /// <summary>
        /// Number of sparks
        /// </summary>
        public int Count = 3;

        /// <summary>
        /// Chance for sparks to occur
        /// </summary>
        public float Prob = 1f;

        /// <summary>
        /// Chance a successful spark hit will also spawn a tile fire
        /// </summary>
        public float TileFireChance;

        /// <summary>
        /// Spark prototypes
        /// </summary>
        public EntProtoId SparkPrototype = ESSparksSystem.DefaultSparks;
    }
}
