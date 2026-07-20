using System.Runtime.CompilerServices;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Extensions;

public static class EntityManagerExt
{
    extension(EntityManager entMan)
    {
        /// Spawns an entity from <paramref name="entityPrototypeId"/> at <paramref name="coordinates"/>, guaranteeing
        /// that it has the given component <typeparamref name="T"/>.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity<T> SpawnAtPosition<T>(
            EntProtoId<T> entityPrototypeId,
            EntityCoordinates coordinates
        ) where T : IComponent, new()
        {
            var ret = entMan.SpawnAtPosition(entityPrototypeId, coordinates);
            DebugTools.Assert(entMan.HasComponent<T>(ret));
            return (ret, entMan.EnsureComponent<T>(ret));
        }

        /// Spawns an entity from <paramref name="entityPrototypeId"/> at <paramref name="coordinates"/>, guaranteeing
        /// that it has the given component <typeparamref name="T"/>.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity<T> PredictedSpawnAtPosition<T>(
            EntProtoId<T> entityPrototypeId,
            EntityCoordinates coordinates
        ) where T : IComponent, new()
        {
            var ret = entMan.PredictedSpawnAtPosition(entityPrototypeId, coordinates);
            DebugTools.Assert(entMan.HasComponent<T>(ret));
            return (ret, entMan.EnsureComponent<T>(ret));
        }
    }
}
