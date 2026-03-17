using AliasGenerator.Models;

namespace AliasGenerator.Services;

/// <summary>
/// Generates unique aliases for mapped account/counterparty pairs.
/// </summary>
public interface IAliasGenerator
{
    /// <summary>
    /// Generate a unique alias for every mapped account/counterparty pair.
    /// </summary>
    /// <param name="data">The fully loaded data store.</param>
    /// <returns>
    /// An ordered list of alias results — one per mapping,
    /// with duplicates resolved by appending a numeric suffix.
    /// </returns>
    IReadOnlyList<AliasResult> GenerateAliases(DataStore data);
}
