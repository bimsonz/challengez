using AliasGenerator.Models;

namespace AliasGenerator.Services;

/// <summary>
/// Produces unique aliases for account/counterparty pairs by joining
/// counterparty metadata with account details and resolving duplicates
/// with an incrementing numeric suffix.
/// </summary>
public sealed class AliasGeneratorService : IAliasGenerator
{
    private const int MinStrippedNameLength = 4;

    public IReadOnlyList<AliasResult> GenerateAliases(DataStore data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var accountsByNumber = data.Accounts.ToDictionary(a => a.AccountNumber);
        var counterpartiesByCode = data.Counterparties.ToDictionary(c => c.Code);

        var aliasCounts = new Dictionary<string, int>(data.Mappings.Count);
        var results = new List<AliasResult>(data.Mappings.Count);

        foreach (var mapping in data.Mappings)
        {
            var account = accountsByNumber[mapping.AccountNumber];
            var counterparty = counterpartiesByCode[mapping.CounterpartyCode];

            var baseAlias = BuildAlias(account, counterparty);
            var alias = ResolveAlias(baseAlias, aliasCounts);

            results.Add(new AliasResult(mapping.CounterpartyCode, mapping.AccountNumber, alias));
        }

        return results;
    }

    private static string BuildAlias(Account account, Counterparty counterparty)
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

        var first4 = strippedName[..4];
        var last4 = account.AccountNumber[^4..];

        var suffix = account.AccountType switch
        {
            AccountType.Standard => "-ST",
            AccountType.Suspense => "-SU",
            _ => throw new ArgumentOutOfRangeException(
                nameof(account.AccountType),
                account.AccountType,
                $"Unknown AccountType: {account.AccountType}")
        };

        return $"{counterparty.Code}{counterparty.Jurisdiction}_{account.Currency}{first4}{last4}{suffix}";
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
