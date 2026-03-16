namespace AliasGenerator.Models;

public record DataStore(
    List<Account> Accounts,
    List<Counterparty> Counterparties,
    List<AccountMapping> Mappings
);
