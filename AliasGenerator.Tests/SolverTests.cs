using AliasGenerator.Models;

namespace AliasGenerator.Tests;

public class SolverTests
{
    [Fact]
    public void GenerateAliases_DelegatesToServiceAndProducesResults()
    {
        var data = new DataStore(
            [new Account("testname", "12345678", AccountType.Standard, "USD")],
            [new Counterparty("testcounterpartynamepadding123", "ABCD", "GB")],
            [new AccountMapping("12345678", "ABCD")]);

        var results = Solver.GenerateAliases(data).ToList();

        var result = Assert.Single(results);
        Assert.Equal("ABCDGB_USDtest5678-ST", result.Alias);
    }

    [Fact]
    public void GenerateAliases_WithMultipleMappings_ReturnsAllResults()
    {
        var data = new DataStore(
            [
                new Account("alphaone", "11111111", AccountType.Standard, "GBP"),
                new Account("betatwox", "22222222", AccountType.Suspense, "EUR")
            ],
            [new Counterparty("counterpartyname123456789012", "WXYZ", "US")],
            [
                new AccountMapping("11111111", "WXYZ"),
                new AccountMapping("22222222", "WXYZ")
            ]);

        var results = Solver.GenerateAliases(data).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("WXYZUS_GBPalph1111-ST", results[0].Alias);
        Assert.Equal("WXYZUS_EURbeta2222-SU", results[1].Alias);
    }
}
