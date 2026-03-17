using AliasGenerator.Models;

namespace AliasGenerator.Services;

/// <summary>
/// Produces unique aliases for account/counterparty pairs using a three-phase
/// pipeline: validate and pre-compute fragments, build base aliases in parallel,
/// then resolve duplicates sequentially.
/// </summary>
public sealed class AliasGeneratorService : IAliasGenerator
{
    private const int MinStrippedNameLength = 4;

    /// <summary>
    /// Threshold below which sequential processing outperforms parallel.
    /// At small mapping counts, thread-pool dispatch overhead (~1-5μs per work item)
    /// exceeds the per-item work (~10-50ns for a string.Concat of pre-computed parts).
    /// </summary>
    private const int ParallelThreshold = 1_000;

    public IReadOnlyList<AliasResult> GenerateAliases(DataStore data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Mappings.Count == 0)
            return [];

        // Phase 1: validate all entities and pre-compute reusable fragments
        var accountFragments = PreComputeAccountFragments(data.Accounts);
        var counterpartyPrefixes = PreComputeCounterpartyPrefixes(data.Counterparties);
        ValidateMappings(data.Mappings, accountFragments, counterpartyPrefixes);

        // Phase 2: build base aliases — stateless per iteration, safe to parallelise
        var baseAliases = BuildBaseAliases(data.Mappings, accountFragments, counterpartyPrefixes);

        // Phase 3: resolve duplicates (must be sequential — suffix numbering depends on encounter order)
        var aliasCounts = new Dictionary<string, int>(data.Mappings.Count);
        var results = new List<AliasResult>(data.Mappings.Count);

        for (var i = 0; i < data.Mappings.Count; i++)
        {
            var alias = ResolveAlias(baseAliases[i], aliasCounts);
            results.Add(new AliasResult(
                data.Mappings[i].CounterpartyCode,
                data.Mappings[i].AccountNumber,
                alias));
        }

        return results;
    }

    /// <summary>
    /// Build base alias strings from pre-computed fragments. Uses <see cref="Parallel.For"/>
    /// when the mapping count exceeds <see cref="ParallelThreshold"/>, as each iteration
    /// reads from immutable dictionaries and writes to its own array index (zero contention).
    /// Falls back to sequential for small datasets where thread-pool overhead dominates.
    /// </summary>
    private static string[] BuildBaseAliases(
        List<AccountMapping> mappings,
        Dictionary<string, string> accountFragments,
        Dictionary<string, string> counterpartyPrefixes)
    {
        var baseAliases = new string[mappings.Count];

        if (mappings.Count >= ParallelThreshold)
        {
            Parallel.For(0, mappings.Count, i =>
            {
                var mapping = mappings[i];
                baseAliases[i] = string.Concat(
                    counterpartyPrefixes[mapping.CounterpartyCode],
                    accountFragments[mapping.AccountNumber]);
            });
        }
        else
        {
            for (var i = 0; i < mappings.Count; i++)
            {
                var mapping = mappings[i];
                baseAliases[i] = string.Concat(
                    counterpartyPrefixes[mapping.CounterpartyCode],
                    accountFragments[mapping.AccountNumber]);
            }
        }

        return baseAliases;
    }

    /// <summary>
    /// Pre-compute the account portion of each alias: {Currency}{First4Letters}{Last4Digits}{TypeSuffix}.
    /// Validates all account names upfront — fails fast before any alias generation.
    /// </summary>
    private static Dictionary<string, string> PreComputeAccountFragments(List<Account> accounts)
    {
        var fragments = new Dictionary<string, string>(accounts.Count);

        foreach (var account in accounts)
        {
            var strippedName = account.Name
                .Replace(" ", "", StringComparison.Ordinal)
                .Replace("-", "", StringComparison.Ordinal);

            if (strippedName.Length < MinStrippedNameLength)
            {
                throw new InvalidOperationException(
                    $"Account '{account.AccountNumber}' name '{account.Name}' yields fewer than " +
                    $"{MinStrippedNameLength} letters after removing spaces and hyphens (got '{strippedName}').");
            }

            var suffix = account.AccountType switch
            {
                AccountType.Standard => "-ST",
                AccountType.Suspense => "-SU",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(account.AccountType),
                    account.AccountType,
                    $"Unknown AccountType: {account.AccountType}")
            };

            fragments[account.AccountNumber] = string.Concat(
                account.Currency,
                strippedName.AsSpan(0, 4),
                account.AccountNumber.AsSpan(account.AccountNumber.Length - 4),
                suffix);
        }

        return fragments;
    }

    /// <summary>
    /// Pre-compute the counterparty portion of each alias: {Code}{Jurisdiction}_.
    /// </summary>
    private static Dictionary<string, string> PreComputeCounterpartyPrefixes(List<Counterparty> counterparties)
    {
        var prefixes = new Dictionary<string, string>(counterparties.Count);

        foreach (var cp in counterparties)
        {
            prefixes[cp.Code] = $"{cp.Code}{cp.Jurisdiction}_";
        }

        return prefixes;
    }

    /// <summary>
    /// Validate that every mapping references a known account and counterparty.
    /// Produces clear domain-specific errors rather than opaque KeyNotFoundException.
    /// </summary>
    private static void ValidateMappings(
        List<AccountMapping> mappings,
        Dictionary<string, string> accountFragments,
        Dictionary<string, string> counterpartyPrefixes)
    {
        foreach (var mapping in mappings)
        {
            if (!accountFragments.ContainsKey(mapping.AccountNumber))
            {
                throw new InvalidOperationException(
                    $"Mapping references unknown account number '{mapping.AccountNumber}'.");
            }

            if (!counterpartyPrefixes.ContainsKey(mapping.CounterpartyCode))
            {
                throw new InvalidOperationException(
                    $"Mapping references unknown counterparty code '{mapping.CounterpartyCode}'.");
            }
        }
    }

    private static string ResolveAlias(string baseAlias, Dictionary<string, int> aliasCounts)
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
