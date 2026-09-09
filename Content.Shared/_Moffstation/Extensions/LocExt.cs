using System.Runtime.CompilerServices;

namespace Content.Shared._Moffstation.Extensions;

public static class LocExt
{
    extension(ILocalizationManager loc)
    {
        /// <see cref="LocalizationManager.GetString(String)"/>, but accepts null, returning null.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string? GetStringOrNull(LocId? locId) => locId is { } id ? loc.GetString(id) : null;
    }
}
