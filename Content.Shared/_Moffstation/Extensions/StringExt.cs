using System.Text.RegularExpressions;

namespace Content.Shared._Moffstation.Extensions;

public static partial class StringExt
{
#pragma warning disable SYSLIB1045
    // Disallowed by Robust sandboxing
    private static readonly Regex CamelCaseRegex = new("(?<!\\b)(?:[A-Z]+(?![a-z])|[A-Z](?=[a-z]))");
#pragma warning restore SYSLIB1045

    extension(string str)
    {
        /// Converts a <c>camelCase</c> or <c>PascalCase</c> string to <c>kebab-case</c> and makes all characters
        /// lowercase. Sequences of uppercase letters are treated as separate words, so <c>ABCase</c> becomes
        /// <c>a-b-case</c>.
        public string CamelCaseToKebabCase()
        {
            return CamelCaseRegex.Replace(str, match => $"-{match.Value}").ToLowerInvariant();
        }

        /// Removes <paramref name="suffix"/> from the end of the string if it exists.
        public string TrimSuffix(
            string suffix,
            StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase
        ) => str.EndsWith(suffix, comparisonType) ? str[..^suffix.Length] : str;

        /// Removes <paramref name="prefix"/> from the beginning of the string if it exists.
        public string TrimPrefix(
            string prefix,
            StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase
        ) => str.StartsWith(prefix) ? str[prefix.Length..] : str;

        /// Literally just sequential application of <see cref="TrimSuffix"/> and <see cref="TrimPrefix"/>.
        public string Trim(
            string affix,
            StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase
        ) => str.TrimSuffix(affix, comparisonType).TrimPrefix(affix, comparisonType);
    }
}
