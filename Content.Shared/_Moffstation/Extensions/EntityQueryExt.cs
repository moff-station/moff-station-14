namespace Content.Shared._Moffstation.Extensions;

public static class EntityQueryExt
{
    extension<T>(EntityQuery<T> query) where T : Component
    {
        /// Resolves <typeparamref name="T"/> on <paramref name="ent"/>, returning an <see cref="Entity{T}"/> if it
        /// exists a null otherwise.
        public Entity<T>? ResolveOrNull(EntityUid? ent, bool logMissing = true)
        {
            if (ent is null)
                return null;

            T? comp = null;
            return query.Resolve(ent.Value, ref comp, logMissing) ? (ent.Value, comp) : null;
        }

        /// Resolves <typeparamref name="T"/> on <paramref name="ent"/>, returning an <see cref="Entity{T}"/> if it
        /// exists a null otherwise.
        public Entity<T>? ResolveOrNull(Entity<T?> ent, bool logMissing = true)
        {
            return query.Resolve(ent, ref ent.Comp, logMissing) ? (ent, ent.Comp) : null;
        }

        /// Resolves <typeparamref name="T"/> on all given <paramref name="entities"/>. Any given entity which does not
        /// have the component is not included in the result.
        public IEnumerable<Entity<T>> ResolveAll(IEnumerable<EntityUid> entities, bool logMissing = true)
        {
            foreach (var entity in entities)
            {
                if (query.ResolveOrNull(entity, logMissing) is { } ent)
                    yield return ent;
            }
        }

        /// Resolves <typeparamref name="T"/> on all given <paramref name="entities"/>. Any given entity which does not
        /// have the component is not included in the result.
        public IEnumerable<Entity<T>> ResolveAll<T2>(IEnumerable<Entity<T2>> entities, bool logMissing = true)
            where T2 : Component
        {
            foreach (var entity in entities)
            {
                if (query.ResolveOrNull(entity, logMissing) is { } ent)
                    yield return ent;
            }
        }

        /// Resolves <typeparamref name="T"/> on all given <paramref name="entities"/>. Any given entity which does not
        /// have the component is not included in the result.
        public IEnumerable<Entity<T>> ResolveAll(IEnumerable<Entity<T?>> entities, bool logMissing = true)
        {
            foreach (var entity in entities)
            {
                if (query.ResolveOrNull(entity, logMissing) is { } ent)
                    yield return ent;
            }
        }
    }
}
