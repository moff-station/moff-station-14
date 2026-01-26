using System.Runtime.CompilerServices;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Extensions;

public static partial class PriorityQueueExt
{
    /// <see cref="PriorityQueue{T}.Take"/>, but returns null if the queue is empty, instead of throwing.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? TakeOrNull<T>(this PriorityQueue<T> queue) where T : struct
    {
        return queue.Count != 0 ? queue.Take() : null;
    }
}
