using System.Runtime.CompilerServices;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Extensions;

public static class EntitySystemExt
{
    /// Throws a debug assert and logs the given <paramref name="message"/> to the system's error logger.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertOrLogError(this EntitySystem entSys, string message)
    {
        DebugTools.Assert(message);
        entSys.Log.Error(message);
    }

    /// <see cref="AssertOrLogError"/>, but returns <paramref name="ret"/> instead of void.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T AssertOrLogError<T>(this EntitySystem entSys, string message, T ret)
    {
        DebugTools.Assert(message);
        entSys.Log.Error(message);
        return ret;
    }
}
