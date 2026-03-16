using AliasGenerator.Models;

namespace AliasGenerator;

public static class Solver
{
    /// <summary>
    /// Generate a unique alias for every mapped account/counterparty pair.
    /// </summary>
    /// <param name="data">The fully loaded data store.</param>
    /// <returns>
    /// A collection of <see cref="AliasResult"/> — one per mapping,
    /// with duplicates resolved by appending a numeric suffix.
    /// </returns>
    public static IEnumerable<AliasResult> GenerateAliases(DataStore data)
    {
        // TODO: implement alias generation
        throw new NotImplementedException();
    }
}

/// <param name="CounterpartyCode">The 4-letter counterparty code.</param>
/// <param name="AccountNumber">The 8-digit account number.</param>
/// <param name="Alias">The generated alias string.</param>
public record AliasResult(
    string CounterpartyCode,
    string AccountNumber,
    string Alias
);
