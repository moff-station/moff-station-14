using System.Runtime.CompilerServices;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Extensions;

public static partial class PriorityQueueExt
{
    extension<T>(PriorityQueue<T> queue) where T : struct
    {
        /// <see cref="PriorityQueue{T}.Take"/>, but returns <c>null</c> if the queue is empty.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? TakeOrNull() => queue.Count == 0 ? null : queue.Take();
    }
}
