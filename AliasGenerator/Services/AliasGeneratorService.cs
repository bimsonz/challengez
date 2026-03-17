using AliasGenerator.Models;

namespace AliasGenerator.Services;

/// <summary>
/// Orchestrates alias generation through a three-phase pipeline:
/// validate and pre-compute fragments, build base aliases, then resolve duplicates.
/// </summary>
public sealed class AliasGeneratorService : IAliasGenerator
{
    public IReadOnlyList<AliasResult> GenerateAliases(DataStore data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Mappings.Count == 0)
        {
            return [];
        }

        // Phase 1: validate and pre-compute reusable fragments (validation happens during computation)
        var accountFragments = FragmentPreComputer.ComputeAccountFragments(data.Accounts);
        var counterpartyPrefixes = FragmentPreComputer.ComputeCounterpartyPrefixes(data.Counterparties);
        DataValidator.ValidateMappings(data.Mappings, accountFragments, counterpartyPrefixes);

        // Phase 2: build base aliases from pre-computed fragments
        var baseAliases = new string[data.Mappings.Count];

        for (var i = 0; i < data.Mappings.Count; i++)
        {
            var mapping = data.Mappings[i];
            baseAliases[i] = string.Concat(
                counterpartyPrefixes[mapping.CounterpartyCode],
                accountFragments[mapping.AccountNumber]);
        }

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
}
