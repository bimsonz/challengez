using AliasGenerator.Models;
using AliasGenerator.Services;

namespace AliasGenerator;

public static class Solver
{
    private static readonly IAliasGenerator Generator = new AliasGeneratorService();

    /// <summary>
    /// Generate a unique alias for every mapped account/counterparty pair.
    /// </summary>
    /// <param name="data">The fully loaded data store.</param>
    /// <returns>
    /// A collection of <see cref="AliasResult"/> — one per mapping,
    /// with duplicates resolved by appending a numeric suffix.
    /// </returns>
    public static IReadOnlyList<AliasResult> GenerateAliases(DataStore data)
    {
        return Generator.GenerateAliases(data);
    }
}
