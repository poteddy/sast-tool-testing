using Newtonsoft.Json;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;

namespace ToolTester.Parsers.Checkmarx;

public sealed partial class Parser : IParser
{
    private bool _disposed;

    public IReadOnlyList<string> GetScanTypes() => ["Checkmarx One JSON"];
    public string GetLabelForScanType(string scanType) => scanType;
    public string GetDescriptionForScanType(string scanType) =>
        "Checkmarx One JSON export containing a top-level results array.";

    public Task<List<CWEs>> Get_findings(Stream fs) => GetFindingsAsync(fs);
    List<int> IParser.Get_findings(Stream fs)
    {
        throw new NotImplementedException();
    }
    public async Task<List<CWEs>> GetFindingsAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<CWEs>();
        await foreach (var finding in ReadFindingsAsync(stream, cancellationToken))
        {
            findings.Add(finding);
        }
        return findings;
    }

    public async IAsyncEnumerable<CWEs> ReadFindingsAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(stream);

        using var textReader = new StreamReader(
            stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
            bufferSize: 64 * 1024, leaveOpen: true);
        using var jsonReader = new JsonTextReader(textReader)
        {
            CloseInput = false,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            FloatParseHandling = FloatParseHandling.Decimal,
            SupportMultipleContent = false
        };

        var serializer = JsonSerializer.CreateDefault();
        if (!await MoveToResultsArrayAsync(jsonReader, cancellationToken))
        {
            throw new JsonSerializationException("The Checkmarx document does not contain a top-level 'results' array.");
        }

        while (await jsonReader.ReadAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (jsonReader.TokenType == JsonToken.EndArray)
            {
                yield break;
            }
            if (jsonReader.TokenType != JsonToken.StartObject)
            {
                continue;
            }

            var result = serializer.Deserialize<CheckmarxResult>(jsonReader);
            if (result is not null && string.Equals(result.Type, "sast", StringComparison.OrdinalIgnoreCase))
            {
                yield return MapFinding(result);
            }
        }
    }

    private static async Task<bool> MoveToResultsArrayAsync(
        JsonTextReader reader,
        CancellationToken cancellationToken)
    {
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.TokenType != JsonToken.PropertyName ||
                !string.Equals(reader.Value?.ToString(), "results", StringComparison.Ordinal))
            {
                continue;
            }

            return await reader.ReadAsync(cancellationToken) && reader.TokenType == JsonToken.StartArray;
        }
        return false;
    }

    private static CWEs MapFinding(CheckmarxResult result)
    {
        var nodes = result.Data?.Nodes ?? [];
        var primary = nodes.LastOrDefault() ?? nodes.FirstOrDefault();
        var description = Decode(result.Description);
        var title = Truncate(
            FirstNonEmpty(result.Data?.QueryName, result.Label, description, result.Id, "Checkmarx finding"),
            150);
        var newresult = new CWEs(title: title
          , test: 4
          , numericalSeverity: "100"
          , foundBy: new List<int?>() { 1 }
          , severity: NormalizeSeverity(result.Severity)
          , description: BuildDescription(result, nodes)
          , staticFinding: true
          , dynamicFinding: false
          , filePath: NormalizePath(primary?.FileName)
          , line: primary?.Line
          , references: ""
          
          );

        newresult.Cve = TryExtractCve(description) ?? TryExtractCve(result.Label);
        newresult.Cwe = result.VulnerabilityDetails.CweId.Value;
        var sinceDate = result.FirstFoundAt ?? result.FoundAt ?? result.Created;
        newresult.Date = DateTime.Parse(sinceDate.Value.ToUniversalTime().ToString());

        return newresult;
      
    }

    private static string BuildDescription(CheckmarxResult result, IReadOnlyList<CheckmarxNode> nodes)
    {
        var builder = new StringBuilder();
        AppendLine(builder, "Description", Decode(result.Description));
        AppendLine(builder, "Query", result.Data?.QueryName);
        AppendLine(builder, "Group", result.Data?.Group);
        AppendLine(builder, "Language", result.Data?.LanguageName);
        AppendLine(builder, "Status", result.Status);
        AppendLine(builder, "State", result.State);

        if (nodes.Count > 0)
        {
            builder.AppendLine("**Data flow:**");
            foreach (var node in nodes)
            {
                var path = NormalizePath(node.FileName) ?? "Unknown file";
                var position = node.Line is null ? path : $"{path}:{node.Line}:{node.Column ?? 0}";
                builder.Append("- ").Append(position);
                if (!string.IsNullOrWhiteSpace(node.Name)) builder.Append(" - ").Append(Decode(node.Name));
                if (!string.IsNullOrWhiteSpace(node.FullName)) builder.Append(" (").Append(Decode(node.FullName)).Append(')');
                builder.AppendLine();
            }
        }

        var compliances = result.VulnerabilityDetails?.Compliances;
        if (compliances is { Count: > 0 })
        {
            builder.Append("**Compliances:** ").AppendLine(string.Join(", ", compliances));
        }
        return builder.ToString().TrimEnd();
    }

    private static void AppendLine(StringBuilder builder, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            builder.Append("**").Append(label).Append(":** ").AppendLine(value);
    }

    private static string NormalizeSeverity(string? value) => value?.ToUpperInvariant() switch
    {
        "CRITICAL" => "Critical",
        "HIGH" => "High",
        "MEDIUM" => "Medium",
        "LOW" => "Low",
        "INFO" or "INFORMATION" => "Info",
        _ => "Medium"
    };

    private static string? NormalizePath(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Replace('\\', '/');

    private static string? Decode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : WebUtility.HtmlDecode(value).Trim();

    private static string FirstNonEmpty(params string?[] values) =>
        values.First(value => !string.IsNullOrWhiteSpace(value))!;

    private static string? FirstNonEmptyOrNull(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static string? TryExtractCve(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var match = CveRegex().Match(value);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    [GeneratedRegex(@"CVE-\d{4}-\d+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CveRegex();

    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
