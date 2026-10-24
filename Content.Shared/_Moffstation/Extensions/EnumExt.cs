using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._Moffstation.Extensions;

public static class EnumExt
{
    extension<TEnum>(TEnum e) where TEnum : Enum
    {
        /// Throws, complaining that the given variant of <typeparamref name="TEnum"/>, <paramref name="e"/>, is unknown.
        [DoesNotReturn]
        public void ThrowUnknownVariant() => e.ThrowUnknownVariant<TEnum, int>();

        /// Throws, complaining that the given variant of <typeparamref name="TEnum"/>, <paramref name="e"/>, is
        /// unknown. "returns" <typeparamref name="TRet"/> to appease the One True God, the typechecker.
        [DoesNotReturn]
        public TRet ThrowUnknownVariant<TRet>() => throw new(
            $"Unreachable: {e.ToString()} is an unknown variant of enum {typeof(TEnum).FullName}."
        );
    }
}
