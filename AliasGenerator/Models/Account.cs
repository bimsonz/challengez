namespace AliasGenerator.Models;

public enum AccountType
{
    Standard,
    Suspense
}

public record Account(
    string Name,
    string AccountNumber,
    AccountType AccountType,
    string Currency
);
