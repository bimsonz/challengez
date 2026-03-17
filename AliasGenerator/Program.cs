using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AliasGenerator;
using AliasGenerator.Models;

if (args is not [var path, ..])
{
    throw new ArgumentException("Usage: AliasGenerator <path-to-input-file>");
}

if (!File.Exists(path))
{
    throw new FileNotFoundException($"Input file not found: {path}", path);
}

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};

await using var stream = File.OpenRead(path);
var data = await JsonSerializer.DeserializeAsync<DataStore>(stream, options);

if (data is null)
{
    throw new InvalidOperationException("Failed to deserialise data file.");
}

Console.Error.WriteLine($"Loaded {data.Accounts.Count} accounts, {data.Counterparties.Count} counterparties, {data.Mappings.Count} mappings.");

Console.Error.WriteLine($"[START] {DateTime.UtcNow:O}");
var stopwatch = Stopwatch.StartNew();

var results = Solver.GenerateAliases(data);

stopwatch.Stop();
Console.Error.WriteLine($"[END]   {DateTime.UtcNow:O}");
Console.Error.WriteLine($"[TIME]  {stopwatch.Elapsed.TotalMicroseconds:F0}μs ({stopwatch.ElapsedMilliseconds}ms)");

using var writer = new StreamWriter(Console.OpenStandardOutput(), leaveOpen: true);
writer.AutoFlush = false;

foreach (var result in results)
{
    writer.Write(result.CounterpartyCode);
    writer.Write(',');
    writer.Write(result.AccountNumber);
    writer.Write(',');
    writer.WriteLine(result.Alias);
}
