using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AliasGenerator;
using AliasGenerator.Models;

var path = "data.json";

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};

await using var stream = File.OpenRead(path);
var data = await JsonSerializer.DeserializeAsync<DataStore>(stream, options)
    ?? throw new InvalidOperationException("Failed to deserialise data file.");

Console.WriteLine($"Loaded {data.Accounts.Count} accounts, {data.Counterparties.Count} counterparties, {data.Mappings.Count} mappings.");

Console.WriteLine($"[START] {DateTime.UtcNow:O}");
var stopwatch = Stopwatch.StartNew();

var results = Solver.GenerateAliases(data).ToList();

stopwatch.Stop();
Console.WriteLine($"[END]   {DateTime.UtcNow:O}");
Console.WriteLine($"[TIME]  {stopwatch.ElapsedMilliseconds}ms");

foreach (var result in results)
    Console.WriteLine($"{result.CounterpartyCode},{result.AccountNumber},{result.Alias}");

return 0;
