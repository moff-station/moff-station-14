using System.Linq;
using System.Runtime.CompilerServices;

namespace Content.Shared._Moffstation.Extensions;

public static class IEnumerableExt
{
    extension<T>(IEnumerable<T> e) where T : struct
    {
        /// If <paramref name="item"/> is not null, appends it to <paramref name="e"/>, otherwise returns <paramref name="e"/>.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> AppendNotNull(T? item) => item != null ? e.Append(item.Value) : e;
    }

    extension<T>(IEnumerable<Entity<T>> e) where T : Component
    {
        /// Maps each entity in <paramref name="e"/> to its owner.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<EntityUid> Owners() => e.Select(it => it.Owner);
    }

    extension<T>(IEnumerable<Entity<T?>> e) where T : Component
    {
        /// <see cref="Owners"/>, but for nullable <typeparamref name="T"/>.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<EntityUid> OwnersNullable() => e.Select(it => it.Owner);
    }
}
