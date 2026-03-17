using AliasGenerator.Models;

namespace AliasGenerator.Services;

/// <summary>
/// Validates mapping integrity before alias generation begins.
/// Ensures all mappings reference known entities.
/// </summary>
public static class DataValidator
{
    /// <summary>
    /// Validate that every mapping references a known account and counterparty.
    /// Produces clear domain-specific errors rather than opaque KeyNotFoundException.
    /// </summary>
    public static void ValidateMappings(
        IReadOnlyList<AccountMapping> mappings,
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
