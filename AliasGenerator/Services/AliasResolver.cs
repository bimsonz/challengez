namespace AliasGenerator.Services;

/// <summary>
/// Resolves duplicate aliases by appending an incrementing numeric suffix.
/// First occurrence keeps the original alias; subsequent occurrences get _1, _2, etc.
/// </summary>
public static class AliasResolver
{
    /// <summary>
    /// Track the base alias and return the resolved alias with a duplicate suffix if needed.
    /// Must be called sequentially in mapping order — suffix numbering depends on encounter order.
    /// </summary>
    public static string Resolve(string baseAlias, Dictionary<string, int> aliasCounts)
    {
        if (aliasCounts.TryGetValue(baseAlias, out var count))
        {
            aliasCounts[baseAlias] = count + 1;
            return $"{baseAlias}_{count}";
        }

        aliasCounts[baseAlias] = 1;
        return baseAlias;
    }
}
