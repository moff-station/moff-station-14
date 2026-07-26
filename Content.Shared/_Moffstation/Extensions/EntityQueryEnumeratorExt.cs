using JetBrains.Annotations;

namespace Content.Shared._Moffstation.Extensions;

public static class EntityQueryEnumeratorExt
{
    extension<T>(EntityQueryEnumerator<T> q) where T : Component
    {
        /// Wraps <see cref="EntityQueryEnumerator{T}.MoveNext(out EntityUid, out T)"/> in an
        /// <see cref="IEnumerable{Entity{T}}"/>, making it a little bit nicer to use.
        [MustUseReturnValue]
        public IEnumerable<Entity<T>> AsEnumerable()
        {
            while (q.MoveNext(out var uid, out var comp))
            {
                yield return (uid, comp);
            }
        }
    }

    extension<T1, T2>(EntityQueryEnumerator<T1, T2> q) where T1 : Component where T2 : Component
    {
        /// Wraps <see cref="EntityQueryEnumerator{T1, T2}.MoveNext(out EntityUid, out T1, out T2)"/> in an
        /// <see cref="IEnumerable{Entity{T1, T2}}"/>, making it a little bit nicer to use.
        [MustUseReturnValue]
        public IEnumerable<Entity<T1, T2>> AsEnumerable()
        {
            while (q.MoveNext(out var uid, out var comp1, out var comp2))
            {
                yield return (uid, comp1, comp2);
            }
        }
    }

    /// Wraps <see cref="AllEntityQueryEnumerator{T}.MoveNext(out EntityUid, out T)"/> in an
    /// <see cref="IEnumerable{Entity{T}}"/>, making it a little bit nicer to use.
    extension<T>(AllEntityQueryEnumerator<T> q) where T : Component
    {
        /// Wraps <see cref="AllEntityQueryEnumerator{T}.MoveNext(out EntityUid, out T)"/> in an
        /// <see cref="IEnumerable{Entity{T}}"/>, making it a little bit nicer to use.
        [MustUseReturnValue]
        public IEnumerable<Entity<T>> AsEnumerable()
        {
            while (q.MoveNext(out var uid, out var comp))
            {
                yield return (uid, comp);
            }
        }
    }
}
