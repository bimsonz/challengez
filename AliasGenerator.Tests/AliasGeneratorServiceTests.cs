using AliasGenerator.Models;
using AliasGenerator.Services;

namespace AliasGenerator.Tests;

public class AliasGeneratorServiceTests
{
    private readonly AliasGeneratorService _sut = new();

    #region Alias Format

    [Fact]
    public void GenerateAliases_SingleMapping_ProducesCorrectFormat()
    {
        var data = CreateDataStore(
            [MakeAccount(name: "testname", number: "12345678", currency: "USD")],
            [MakeCounterparty(code: "ABCD", jurisdiction: "GB")],
            [new AccountMapping("12345678", "ABCD")]);

        var results = _sut.GenerateAliases(data);

        var result = Assert.Single(results);
        Assert.Equal("ABCDGB_USDtest5678-ST", result.Alias);
        Assert.Equal("ABCD", result.CounterpartyCode);
        Assert.Equal("12345678", result.AccountNumber);
    }

    [Fact]
    public void GenerateAliases_ReadmeExample_ProducesExpectedAlias()
    {
        // Exact example from the README specification
        var data = CreateDataStore(
            [MakeAccount(name: "ab-c dxy", number: "12345678", currency: "USD")],
            [MakeCounterparty(code: "ABCD", jurisdiction: "GB")],
            [new AccountMapping("12345678", "ABCD")]);

        var results = _sut.GenerateAliases(data);

        Assert.Equal("ABCDGB_USDabcd5678-ST", Assert.Single(results).Alias);
    }

    [Fact]
    public void GenerateAliases_StandardAccount_HasStSuffix()
    {
        var data = CreateDataStore(
            [MakeAccount(type: AccountType.Standard)],
            [MakeCounterparty()],
            [new AccountMapping("12345678", "ABCD")]);

        var results = _sut.GenerateAliases(data);

        Assert.EndsWith("-ST", Assert.Single(results).Alias);
    }

    [Fact]
    public void GenerateAliases_SuspenseAccount_HasSuSuffix()
    {
        var data = CreateDataStore(
            [MakeAccount(type: AccountType.Suspense)],
            [MakeCounterparty()],
            [new AccountMapping("12345678", "ABCD")]);

        var results = _sut.GenerateAliases(data);

        Assert.EndsWith("-SU", Assert.Single(results).Alias);
    }

    [Fact]
    public void GenerateAliases_StripsSpacesAndHyphensFromName()
    {
        var data = CreateDataStore(
            [MakeAccount(name: "a-b cdef")],
            [MakeCounterparty()],
            [new AccountMapping("12345678", "ABCD")]);

        var results = _sut.GenerateAliases(data);

        // "a-b cdef" → "abcdef" → first 4 = "abcd"
        Assert.Contains("abcd", Assert.Single(results).Alias);
    }

    [Fact]
    public void GenerateAliases_UsesCounterpartyCodeAndJurisdiction()
    {
        var data = CreateDataStore(
            [MakeAccount()],
            [MakeCounterparty(code: "WXYZ", jurisdiction: "DE")],
            [new AccountMapping("12345678", "WXYZ")]);

        var results = _sut.GenerateAliases(data);

        Assert.StartsWith("WXYZDE_", Assert.Single(results).Alias);
    }

    [Fact]
    public void GenerateAliases_UsesAccountCurrency()
    {
        var data = CreateDataStore(
            [MakeAccount(currency: "EUR")],
            [MakeCounterparty()],
            [new AccountMapping("12345678", "ABCD")]);

        var results = _sut.GenerateAliases(data);

        Assert.Contains("EUR", Assert.Single(results).Alias);
    }

    [Fact]
    public void GenerateAliases_UsesLast4DigitsOfAccountNumber()
    {
        var data = CreateDataStore(
            [MakeAccount(number: "99998765")],
            [MakeCounterparty()],
            [new AccountMapping("99998765", "ABCD")]);

        var results = _sut.GenerateAliases(data);

        Assert.Contains("8765", Assert.Single(results).Alias);
    }

    #endregion

    #region Duplicate Handling

    [Fact]
    public void GenerateAliases_DuplicateAliases_FirstKeepsOriginal()
    {
        var data = CreateDuplicateScenario();

        var results = _sut.GenerateAliases(data);

        Assert.Equal("ABCDGB_USDabcd1234-ST", results[0].Alias);
    }

    [Fact]
    public void GenerateAliases_DuplicateAliases_SecondGetsSuffix1()
    {
        var data = CreateDuplicateScenario();

        var results = _sut.GenerateAliases(data);

        Assert.Equal("ABCDGB_USDabcd1234-ST_1", results[1].Alias);
    }

    [Fact]
    public void GenerateAliases_ThreeDuplicates_IncrementingSuffixes()
    {
        var data = CreateDuplicateScenario();

        var results = _sut.GenerateAliases(data);

        Assert.Equal(3, results.Count);
        Assert.Equal("ABCDGB_USDabcd1234-ST", results[0].Alias);
        Assert.Equal("ABCDGB_USDabcd1234-ST_1", results[1].Alias);
        Assert.Equal("ABCDGB_USDabcd1234-ST_2", results[2].Alias);
    }

    [Fact]
    public void GenerateAliases_DistinctAliases_NoSuffixes()
    {
        var data = CreateDataStore(
            [
                MakeAccount(name: "testaaaa", number: "11111111", currency: "USD"),
                MakeAccount(name: "testbbbb", number: "22222222", currency: "EUR")
            ],
            [MakeCounterparty()],
            [
                new AccountMapping("11111111", "ABCD"),
                new AccountMapping("22222222", "ABCD")
            ]);

        var results = _sut.GenerateAliases(data);

        Assert.Equal(2, results.Count);
        Assert.DoesNotContain("_1", results[0].Alias);
        Assert.DoesNotContain("_1", results[1].Alias);
    }

    #endregion

    #region Validation and Edge Cases

    [Fact]
    public void GenerateAliases_NameTooShortAfterStripping_ThrowsInvalidOperationException()
    {
        var data = CreateDataStore(
            [MakeAccount(name: "a-b")],
            [MakeCounterparty()],
            [new AccountMapping("12345678", "ABCD")]);

        var ex = Assert.Throws<InvalidOperationException>(() => _sut.GenerateAliases(data));
        Assert.Contains("fewer than 4 letters", ex.Message);
    }

    [Fact]
    public void GenerateAliases_ExactlyFourLettersAfterStripping_Succeeds()
    {
        var data = CreateDataStore(
            [MakeAccount(name: "ab-cd")],
            [MakeCounterparty()],
            [new AccountMapping("12345678", "ABCD")]);

        var results = _sut.GenerateAliases(data);

        Assert.Single(results);
    }

    [Fact]
    public void GenerateAliases_EmptyMappings_ReturnsEmpty()
    {
        var data = CreateDataStore(
            [MakeAccount()],
            [MakeCounterparty()],
            []);

        var results = _sut.GenerateAliases(data);

        Assert.Empty(results);
    }

    [Fact]
    public void GenerateAliases_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.GenerateAliases(null!));
    }

    [Fact]
    public void GenerateAliases_UnknownAccountType_ThrowsArgumentOutOfRangeException()
    {
        var data = CreateDataStore(
            [MakeAccount(type: (AccountType)999)],
            [MakeCounterparty()],
            [new AccountMapping("12345678", "ABCD")]);

        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.GenerateAliases(data));
    }

    [Fact]
    public void GenerateAliases_MappingReferencesUnknownAccount_ThrowsInvalidOperationException()
    {
        var data = CreateDataStore(
            [MakeAccount()],
            [MakeCounterparty()],
            [new AccountMapping("99999999", "ABCD")]);

        var ex = Assert.Throws<InvalidOperationException>(() => _sut.GenerateAliases(data));
        Assert.Contains("unknown account number", ex.Message);
    }

    [Fact]
    public void GenerateAliases_MappingReferencesUnknownCounterparty_ThrowsInvalidOperationException()
    {
        var data = CreateDataStore(
            [MakeAccount()],
            [MakeCounterparty()],
            [new AccountMapping("12345678", "ZZZZ")]);

        var ex = Assert.Throws<InvalidOperationException>(() => _sut.GenerateAliases(data));
        Assert.Contains("unknown counterparty code", ex.Message);
    }

    [Fact]
    public void GenerateAliases_PreservesMappingOrder()
    {
        var data = CreateDataStore(
            [
                MakeAccount(name: "zzzzzzzz", number: "11111111"),
                MakeAccount(name: "aaaaaaaa", number: "22222222")
            ],
            [MakeCounterparty()],
            [
                new AccountMapping("22222222", "ABCD"),
                new AccountMapping("11111111", "ABCD")
            ]);

        var results = _sut.GenerateAliases(data);

        Assert.Equal("22222222", results[0].AccountNumber);
        Assert.Equal("11111111", results[1].AccountNumber);
    }

    [Fact]
    public void GenerateAliases_ManyToMany_GeneratesAliasPerMapping()
    {
        var data = CreateDataStore(
            [MakeAccount(number: "11111111"), MakeAccount(number: "22222222")],
            [
                MakeCounterparty(code: "AAAA", jurisdiction: "GB"),
                MakeCounterparty(code: "BBBB", jurisdiction: "US")
            ],
            [
                new AccountMapping("11111111", "AAAA"),
                new AccountMapping("11111111", "BBBB"),
                new AccountMapping("22222222", "AAAA")
            ]);

        var results = _sut.GenerateAliases(data);

        Assert.Equal(3, results.Count);
        Assert.StartsWith("AAAAGB_", results[0].Alias);
        Assert.StartsWith("BBBBUS_", results[1].Alias);
        Assert.StartsWith("AAAAGB_", results[2].Alias);
    }

    #endregion

    #region Test Helpers

    private static DataStore CreateDataStore(
        List<Account> accounts,
        List<Counterparty> counterparties,
        List<AccountMapping> mappings) =>
        new(accounts, counterparties, mappings);

    private static Account MakeAccount(
        string name = "testname",
        string number = "12345678",
        AccountType type = AccountType.Standard,
        string currency = "USD") =>
        new(name, number, type, currency);

    private static Counterparty MakeCounterparty(
        string name = "testcounterpartynamepadding123",
        string code = "ABCD",
        string jurisdiction = "GB") =>
        new(name, code, jurisdiction);

    /// <summary>
    /// Creates a scenario where three accounts produce identical base aliases:
    /// all strip to first4="abcd", last4="1234", USD, Standard, same counterparty.
    /// </summary>
    private static DataStore CreateDuplicateScenario() =>
        CreateDataStore(
            [
                MakeAccount(name: "abcdefgh", number: "00001234"),
                MakeAccount(name: "ab-c dxy", number: "99991234"),
                MakeAccount(name: "abcdonly", number: "55551234")
            ],
            [MakeCounterparty(code: "ABCD", jurisdiction: "GB")],
            [
                new AccountMapping("00001234", "ABCD"),
                new AccountMapping("99991234", "ABCD"),
                new AccountMapping("55551234", "ABCD")
            ]);

    #endregion
}
