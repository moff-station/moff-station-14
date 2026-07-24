namespace Content.Shared._Moffstation.Extensions;

public static class DictionaryExt
{
    extension<TK, TV>(SortedDictionary<TK, TV> dict) where TK : notnull
    {
        /// Returns the entries in this dictionary whose keys are immediately below and above
        /// <paramref name="comparisonValue"/> as deteremined by the dictionary's comparison function.
        /// If the value is lower than all values in the dictionary, the returned value's <c>below</c> value is null,
        /// and vice-versa for <c>above</c>.
        /// If the dictionary is empty, <c>null</c> is returned for both.
        /// If the value is exactly equal to an entry's key, that entry is returned as both <c>below</c> and <c>above</c>.
        public (KeyValuePair<TK, TV>? below, KeyValuePair<TK, TV>? above) GetContainingRange(TK comparisonValue)
        {
            var comp = dict.Comparer;
            using var e = dict.GetEnumerator();
            if (!e.MoveNext())
            {
                // No first element, the dict is empty.
                return (null, null);
            }

            KeyValuePair<TK, TV>? below = null;
            var above = e.Current;

            while (true)
            {
                switch (comp.Compare(comparisonValue, above.Key))
                {
                    case < 0:
                        // The comparison value is below `above`, we're done. Return `below` and `above` as the range.
                        return (below, above);
                    case > 0:
                        // The comparison value is above `above`. We need to continue;
                        if (!e.MoveNext())
                        {
                            // No more elements above `above`, so return `above` as the bottom of the range with nothing at the top bound.
                            return (above, null);
                        }

                        below = above;
                        above = e.Current;
                        break;
                    case 0:
                        // The comparison value is right on `above`. Return `above` as both the top and bottom of the range this value is in.
                        return (above, above);
                }
            }
        }
    }
}
