using AliasGenerator.Models;

namespace AliasGenerator.Services;

/// <summary>
/// Validates data integrity before alias generation begins.
/// Ensures all account names meet minimum length requirements
/// and all mappings reference known entities.
/// </summary>
public static class DataValidator
{
    private const int MinStrippedNameLength = 4;

    /// <summary>
    /// Validate that every account name yields at least 4 letters after
    /// removing spaces and hyphens. Fails fast before any alias generation.
    /// </summary>
    public static void ValidateAccountNames(List<Account> accounts)
    {
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
        }
    }

    /// <summary>
    /// Validate that every mapping references a known account and counterparty.
    /// Produces clear domain-specific errors rather than opaque KeyNotFoundException.
    /// </summary>
    public static void ValidateMappings(
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
}
