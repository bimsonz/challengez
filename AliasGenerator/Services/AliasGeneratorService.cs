using AliasGenerator.Models;

namespace AliasGenerator.Services;

/// <summary>
/// Orchestrates alias generation: validate and pre-compute fragments,
/// then build and resolve aliases in a single sequential pass.
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

        var accountFragments = FragmentPreComputer.ComputeAccountFragments(data.Accounts);
        var counterpartyPrefixes = FragmentPreComputer.ComputeCounterpartyPrefixes(data.Counterparties);
        DataValidator.ValidateMappings(data.Mappings, accountFragments, counterpartyPrefixes);

        var aliasCounts = new Dictionary<string, int>(data.Mappings.Count);
        var results = new List<AliasResult>(data.Mappings.Count);

        foreach (var mapping in data.Mappings)
        {
            var baseAlias = string.Concat(
                counterpartyPrefixes[mapping.CounterpartyCode],
                accountFragments[mapping.AccountNumber]);

            var alias = AliasResolver.Resolve(baseAlias, aliasCounts);
            results.Add(new AliasResult(mapping.CounterpartyCode, mapping.AccountNumber, alias));
        }

        return results;
    }
}
