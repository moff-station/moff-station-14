using System.Linq;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Moffstation.Extensions;

public static partial class JobUIComparerExt
{
    extension(IEnumerable<JobPrototype> jobs)
    {
        /// Returns the contents of <paramref name="jobs"/>, sorted by a comparer made by
        /// <see cref="JobUIComparer.TryCreate"/>. Returns the receiver enumerable in the case that a comparer cannot
        /// be constructed.
        public IEnumerable<JobPrototype> SortForUi(
            IPrototypeManager protoMan,
            ProtoId<JobWeightPrototype>? jobWeights = null
        ) => JobUIComparer.TryCreate(protoMan, jobWeights, out var comparer) ? jobs.Order(comparer) : jobs;
    }
}
