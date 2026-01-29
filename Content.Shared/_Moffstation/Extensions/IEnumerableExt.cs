namespace Content.Shared._Moffstation.Extensions;

public static class IEnumerableExt
{
    extension<TComp>(IEnumerable<EntityUid> source) where TComp : Component
    {
        public IEnumerable<Entity<TComp>> WithComp(IEntityManager entMan)
        {
            foreach (var ent in source)
            {
                if (!entMan.TryGetComponent(ent, out TComp? comp))
                    continue;

                yield return (ent, comp);
            }
        }
    }
}
