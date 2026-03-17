using AliasGenerator.Models;

namespace AliasGenerator.Services;

/// <summary>
/// Pre-computes reusable string fragments for accounts and counterparties,
/// avoiding redundant string operations when the same entity appears in
/// multiple mappings. Validates entity data during computation — fails fast
/// before any alias generation.
/// </summary>
public static class FragmentPreComputer
{
    private const int MinStrippedNameLength = 4;
    private const int RequiredAccountNumberLength = 8;

    /// <summary>
    /// Pre-compute the account portion of each alias: {Currency}{First4Letters}{Last4Digits}{TypeSuffix}.
    /// Each account is processed exactly once regardless of how many mappings reference it.
    /// Validates account names and numbers during computation.
    /// </summary>
    public static Dictionary<string, string> ComputeAccountFragments(IReadOnlyList<Account> accounts)
    {
        var fragments = new Dictionary<string, string>(accounts.Count);

        foreach (var account in accounts)
        {
            if (account.AccountNumber.Length != RequiredAccountNumberLength)
            {
                throw new InvalidOperationException(
                    $"Account number '{account.AccountNumber}' must be exactly " +
                    $"{RequiredAccountNumberLength} digits.");
            }

            var strippedName = account.Name
                .Replace(" ", "", StringComparison.Ordinal)
                .Replace("-", "", StringComparison.Ordinal);

            if (strippedName.Length < MinStrippedNameLength)
            {
                throw new InvalidOperationException(
                    $"Account '{account.AccountNumber}' name '{account.Name}' yields fewer than " +
                    $"{MinStrippedNameLength} letters after removing spaces and hyphens (got '{strippedName}').");
            }

            var suffix = account.AccountType switch
            {
                AccountType.Standard => "-ST",
                AccountType.Suspense => "-SU",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(account.AccountType),
                    account.AccountType,
                    $"Unknown AccountType: {account.AccountType}")
            };

            fragments[account.AccountNumber] = string.Concat(
                account.Currency,
                strippedName.AsSpan(0, 4),
                account.AccountNumber.AsSpan(account.AccountNumber.Length - 4),
                suffix);
        }

        return fragments;
    }

    /// <summary>
    /// Pre-compute the counterparty portion of each alias: {Code}{Jurisdiction}_.
    /// Each counterparty is processed exactly once regardless of how many mappings reference it.
    /// </summary>
    public static Dictionary<string, string> ComputeCounterpartyPrefixes(IReadOnlyList<Counterparty> counterparties)
    {
        var prefixes = new Dictionary<string, string>(counterparties.Count);

        foreach (var cp in counterparties)
        {
            prefixes[cp.Code] = $"{cp.Code}{cp.Jurisdiction}_";
        }

        return prefixes;
    }
}
