using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Moffstation.Chitter;

/// The ID of a <see cref="ChitterAccount"/>.
[Serializable, NetSerializable]
public readonly record struct ChitterAccountId(int Id)
{
    public const int MaxDigits = 4;

    public string Display(bool withHash = true) => withHash ? $"#{Id:D4}" : Id.ToString("D4");

    public static readonly ChitterAccountId Invalid = new(-1);
}

/// The record of a chitter account.
[DataRecord, Serializable, NetSerializable]
public partial record struct ChitterAccount(
    ChitterAccountId Id,
    string Name,
    string JobTitle,
    ProtoId<ChitterAvatarPrototype> ProfilePictureId
)
{
    /// A default <see cref="ChitterAccount"/> for use in UI displays.
    public static readonly ChitterAccount Unknown = new(
        ChitterAccountId.Invalid,
        Loc.GetString("chitter-unknown-account-name"),
        Loc.GetString("chitter-unknown-account-title"),
        ChitterIdentitySystem.DefaultAvatar
    );
}

/// A function which retrieves the <see cref="ChitterAccount"/> associated with the given <paramref name="id"/>, or null
/// if that account cannot be found.
public delegate ChitterAccount? ChitterAccountLookup(ChitterAccountId id);

/// A <see cref="ChitterAccountLookup"/> which never returns null. Because this may return somewhat bogus data, this
/// should not be used to get critical information. Usually this is used for UI display.
public delegate ChitterAccount InfallibleChitterAccountLookup(ChitterAccountId id);

public static class ChitterAccountLookupExt
{
    extension(IEnumerable<ChitterAccount> accounts)
    {
        /// Returns a <see cref="ChitterAccountLookup"/> which can return accounts in the receiver <paramref name="accounts"/>.
        public ChitterAccountLookup AsLookup()
        {
            // Collect the receiver enumerable into a durable collection. If we don't do this, the first lookup will
            // consume the enumeration and cause subsequent lookups to silently fail.
            var set = accounts.ToHashSet();
            return id => set.FirstOrNull(it => it.Id == id);
        }

        /// <inheritdoc cref="AsLookup"/>. In the case that the given ID is unknown, returns <see cref="ChitterAccount.Unknown"/>.
        public InfallibleChitterAccountLookup AsLookupWithUnknownDefault() =>
            accounts.AsLookup().WithDefault(ChitterAccount.Unknown);
    }

    extension(ChitterAccountLookup lookup)
    {
        /// Extends the receiver <paramref name="lookup"/> such that it returns <paramref name="defaultAccount"/> instead of null.
        public InfallibleChitterAccountLookup WithDefault(ChitterAccount defaultAccount) =>
            id => lookup(id) ?? defaultAccount;
    }
}
