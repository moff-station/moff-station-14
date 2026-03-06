using System.Linq;

namespace Content.Shared._Moffstation.Extensions;

public static class IEnumerableExt
{
    extension<T>(IEnumerable<T> source)
    {
        public IEnumerable<TResult> SelectNotNull<TResult>(Func<T, TResult?> selector) where TResult : struct =>
            source.Select<T, TResult?>(selector).OfType<TResult>();

        public IEnumerable<TResult> SelectNotNull<TResult>(Func<T, TResult?> selector) where TResult : class =>
            source.Select<T, TResult?>(selector).OfType<TResult>();
    }
}
