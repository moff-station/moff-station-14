namespace Content.Shared._Moffstation.Extensions;

/// This interface marks a type as having sealed inheritance, enabling access to
/// <see cref="ISealedInheritanceExt.ThrowUnknownInheritor{TSealed, TRet}(TSealed)"/>
public interface ISealedInheritance;

public static class ISealedInheritanceExt
{
    /// Throws, complaining that <typeparamref name="TSealed"/> is, well, sealed, and the given receiver's type is unknown.
    public static void ThrowUnknownInheritor<TSealed>(this TSealed s) where TSealed : ISealedInheritance =>
        ThrowUnknownInheritor<TSealed, int>(s);

    /// Throws, complaining that <typeparamref name="TSealed"/> is, well, sealed, and the given receiver's type is
    /// unknown. "returns" <typeparamref name="TRet"/> to appease the One True God, the typechecker.
    public static TRet ThrowUnknownInheritor<TSealed, TRet>(this TSealed s) where TSealed : ISealedInheritance =>
        throw new(
            $"Unreachable: {typeof(TSealed).FullName} has sealed inheritance, but {s.GetType().FullName} is unknown."
        );
}
