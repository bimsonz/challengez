using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AliasGenerator;
using AliasGenerator.Models;

var path = args.Length > 0 ? args[0] : "data.json";

if (!File.Exists(path))
{
    Console.Error.WriteLine($"File not found: {path}");
    return 1;
}

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

var results = Solver.GenerateAliases(data);

stopwatch.Stop();
Console.WriteLine($"[END]   {DateTime.UtcNow:O}");
Console.WriteLine($"[TIME]  {stopwatch.Elapsed.TotalMicroseconds:F0}μs ({stopwatch.ElapsedMilliseconds}ms)");

foreach (var result in results)
    Console.WriteLine($"{result.CounterpartyCode},{result.AccountNumber},{result.Alias}");

return 0;
