using AliasGenerator.Models;

namespace AliasGenerator.Services;

/// <summary>
/// Orchestrates alias generation through a three-phase pipeline:
/// validate and pre-compute fragments, build base aliases, then resolve duplicates.
/// </summary>
public sealed class AliasGeneratorService : IAliasGenerator
{
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
        {
            return [];
        }

        // Phase 1: validate all entities and pre-compute reusable fragments
        DataValidator.ValidateAccountNames(data.Accounts);
        var accountFragments = FragmentPreComputer.ComputeAccountFragments(data.Accounts);
        var counterpartyPrefixes = FragmentPreComputer.ComputeCounterpartyPrefixes(data.Counterparties);
        DataValidator.ValidateMappings(data.Mappings, accountFragments, counterpartyPrefixes);

        // Phase 2: build base aliases — stateless per iteration, safe to parallelise
        var baseAliases = BuildBaseAliases(data.Mappings, accountFragments, counterpartyPrefixes);

        // Phase 3: resolve duplicates (must be sequential — suffix numbering depends on encounter order)
        var aliasCounts = new Dictionary<string, int>(data.Mappings.Count);
        var results = new List<AliasResult>(data.Mappings.Count);

        for (var i = 0; i < data.Mappings.Count; i++)
        {
            var alias = AliasResolver.Resolve(baseAliases[i], aliasCounts);
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

            return baseAliases;
        }

        for (var i = 0; i < mappings.Count; i++)
        {
            var mapping = mappings[i];
            baseAliases[i] = string.Concat(
                counterpartyPrefixes[mapping.CounterpartyCode],
                accountFragments[mapping.AccountNumber]);
        }

        return baseAliases;
    }
}
