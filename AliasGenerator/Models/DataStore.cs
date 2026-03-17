namespace AliasGenerator.Models;

public record DataStore(
    IReadOnlyList<Account> Accounts,
    IReadOnlyList<Counterparty> Counterparties,
    IReadOnlyList<AccountMapping> Mappings
);
