namespace AliasGenerator.Models;

/// <summary>
/// Represents a generated alias for a specific account/counterparty pair.
/// </summary>
/// <param name="CounterpartyCode">The 4-letter counterparty code.</param>
/// <param name="AccountNumber">The 8-digit account number.</param>
/// <param name="Alias">The generated alias string.</param>
public record AliasResult(
    string CounterpartyCode,
    string AccountNumber,
    string Alias
);
