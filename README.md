# .NET Coding Challenge: Alias Generator CLI

Build a small .NET CLI tool that generates unique aliases for related account and counterparty pairs.

The goal of the challenge is to assess clean, idiomatic .NET code, correctness, and efficient handling of data in both time and space complexity.

## What the candidate is given

Provide the candidate with:

- A barebones .NET console application they can extend
- A static data file containing the input data for the challenge

## Input data

The static data file should contain:

### Accounts

100 accounts with:

- `Name`: string, up to 8 characters
  - Characters are lowercase `a-z`
  - Around 20% of names should also contain a space and a hyphen somewhere in the name
  - A space and a hyphen must never appear next to each other
- `AccountNumber`: 8-digit numeric string
- `AccountType`: enum with values `Standard` and `Suspense`
- `Currency`: 3-letter ISO currency code

### Counterparties

10 counterparties with:

- `Name`: 30 lowercase `a-z` characters
- `Code`: 4-letter code
- `Jurisdiction`: ISO country code

### Relationships

Use an explicit mapping collection in the same static data file.

Each mapping should link one account to one counterparty using:

- `AccountNumber`
- `CounterpartyCode`

Treat the relationship as many-to-many. An account may map to multiple counterparties, and a counterparty may map to multiple accounts.

The candidate should generate aliases only for the mapped pairs, not for the full Cartesian product.

## Required output

For every mapped account/counterparty pair, generate an alias using the format:

`{CounterpartyCode}{CountryCode}_{Currency}{First4LettersOfAccountName}{Last4DigitsOfAccountNumber}{TypeSuffix}`

Where:

- `CounterpartyCode` = the 4-letter counterparty code
- `CountryCode` = the counterparty jurisdiction country code
- `Currency` = the 3-letter account currency
- `First4LettersOfAccountName` = the first 4 letters from the account name after removing spaces and hyphens
- `Last4DigitsOfAccountNumber` = the final 4 digits of the 8-digit account number
- `TypeSuffix`:
  - `-ST` for `Standard`
  - `-SU` for `Suspense`

### Example

If:

- Counterparty code = `ABCD`
- Country code = `GB`
- Currency = `USD`
- Account name = `ab-c dxy`
- Account number = `12345678`
- Account type = `Standard`

Then:

- Remove spaces and hyphens from account name: `abcdxy`
- Take first 4 letters: `abcd`
- Take last 4 digits of account number: `5678`
- Resulting alias: `ABCDGB_USDabcd5678-ST`

## Duplicate handling

Aliases must be unique across the full generated output.

If duplicate aliases are detected during generation, suffix them with:

`_x`

Where `x` is the duplicate occurrence number.

Suggested behavior:

- First occurrence keeps the original alias
- Second occurrence becomes `_1`
- Third occurrence becomes `_2`
- And so on

Example:

- `ABCDGB_USDabcd5678-ST`
- `ABCDGB_USDabcd5678-ST_1`
- `ABCDGB_USDabcd5678-ST_2`

## Expectations

The implementation should:

- Be written in idiomatic .NET
- Be easy to read and reason about
- Handle the transformation rules correctly
- Generate aliases efficiently in both time and space
- Avoid unnecessary allocations and repeated work where reasonable

## Suggested CLI behavior

The exact CLI design is up to the candidate, but a good baseline is:

`dotnet run -- <path-to-input-file>`

The program should read the input data, generate aliases for all mapped pairs, and print the results to standard output.

## Suggested output shape

You can let the candidate choose the exact output contract, or you can define one up front for easier review.

One simple option is to output one line per generated alias including the source identifiers, for example:

`<CounterpartyCode>,<AccountNumber>,<Alias>`
