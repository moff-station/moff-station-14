using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Extensions;

public static class PrototypeManagerExt
{
    extension(IPrototypeManager protoMan)
    {
        public IEnumerable<T> ResolveAll<T>(IEnumerable<ProtoId<T>> ids) where T : class, IPrototype
        {
            foreach (var id in ids)
            {
                if (protoMan.Resolve(id, out var proto))
                    yield return proto;
            }
        }
    }
}
