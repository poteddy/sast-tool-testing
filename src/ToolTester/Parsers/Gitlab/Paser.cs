using global::ToolTester.Application.Common.Interfaces;
using global::ToolTester.Application.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ToolTester.Parsers.GitLabSast;

/// <summary>
/// Streaming parser for GitLab SAST JSON reports.
///
/// GitLab SAST reports are not SARIF. Findings are stored in the root
/// "vulnerabilities" array.
///
/// This parser streams individual vulnerabilities so that large GitLab
/// reports do not have to be loaded completely into memory.
/// </summary>
public sealed class Parser : IParser
{
    private static readonly Regex CweRegex = new(
        @"(?i)\bCWE[-_:/ ]?(?<id>\d+)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex CveRegex = new(
        @"(?i)\bCVE-\d{4}-\d{4,}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private bool _disposed;

    public object get_scan_types() =>
        new List<object>
        {
            "GitLab SAST"
        };

    public object get_label_for_scan_types(object scan_type) =>
        scan_type;

    public object get_description_for_scan_types(object scan_type) =>
        "GitLab SAST JSON report files, including gl-sast-report.json, can be imported.";

    /// <summary>
    /// Preferred asynchronous API.
    ///
    /// Streams findings from the root vulnerabilities array. If the supplied
    /// document is truncated, findings that were completely parsed before the
    /// truncation are returned.
    /// </summary>
    public async Task<List<CWEs>> Get_findings(Stream stream)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(stream);

        var findings = new List<CWEs>();

        using var streamReader = new StreamReader(
            stream,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);

        using var jsonReader = new JsonTextReader(streamReader)
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Decimal,
            SupportMultipleContent = false,
            CloseInput = false
        };

        try
        {
            await ReadVulnerabilitiesAsync(
                    jsonReader,
                    findings)
                .ConfigureAwait(false);
        }
        catch (JsonReaderException)
        {
            // A partial/truncated report may end in the middle of an object.
            // Retain every complete vulnerability parsed before that point.
        }

        return findings;
    }

    /// <summary>
    /// Compatibility implementation for the existing IParser contract.
    ///
    /// This returns the distinct CWE identifiers found in the report.
    /// Use the public asynchronous Get_findings(Stream) method when complete
    /// finding objects are needed.
    /// </summary>
    List<int> IParser.Get_findings(Stream stream) =>
        Get_findings(stream)
            .GetAwaiter()
            .GetResult()
            .Select(x => x.Cwe)
            .Where(cwe => cwe is > 0)
            .Select(cwe => cwe!.Value)
            .Distinct()
            .ToList();

    private static async Task ReadVulnerabilitiesAsync(
        JsonTextReader reader,
        ICollection<CWEs> findings)
    {
        if (!await ReadToStartObjectAsync(reader).ConfigureAwait(false))
        {
            return;
        }

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader.TokenType == JsonToken.EndObject)
            {
                return;
            }

            if (reader.TokenType != JsonToken.PropertyName)
            {
                continue;
            }

            var propertyName = reader.Value?.ToString();

            if (!await reader.ReadAsync().ConfigureAwait(false))
            {
                return;
            }

            if (propertyName?.Equals(
                    "vulnerabilities",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                await ReadVulnerabilityArrayAsync(
                        reader,
                        findings)
                    .ConfigureAwait(false);

                continue;
            }

            await SkipValueAsync(reader).ConfigureAwait(false);
        }
    }

    private static async Task<bool> ReadToStartObjectAsync(
        JsonTextReader reader)
    {
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                return true;
            }

            if (reader.TokenType is not (
                    JsonToken.Comment or
                    JsonToken.None))
            {
                return false;
            }
        }

        return false;
    }

    private static async Task ReadVulnerabilityArrayAsync(
        JsonTextReader reader,
        ICollection<CWEs> findings)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return;
        }

        if (reader.TokenType != JsonToken.StartArray)
        {
            await SkipValueAsync(reader).ConfigureAwait(false);
            return;
        }

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader.TokenType == JsonToken.EndArray)
            {
                return;
            }

            if (reader.TokenType != JsonToken.StartObject)
            {
                await SkipValueAsync(reader).ConfigureAwait(false);
                continue;
            }

            JObject vulnerability;

            try
            {
                vulnerability = await JObject
                    .LoadAsync(reader)
                    .ConfigureAwait(false);
            }
            catch (JsonReaderException)
            {
                // The file ended in the middle of this vulnerability.
                // Findings already added to the collection remain valid.
                return;
            }

            var finding = CreateFinding(vulnerability);

            if (finding is not null)
            {
                findings.Add(finding);
            }
        }
    }

    private static CWEs? CreateFinding(
        JObject vulnerability)
    {
        if (!IsSastFinding(vulnerability))
        {
            return null;
        }

        var reportedCwe = ExtractReportedCwe(vulnerability);

        // ToolTester findings are CWE-based. Ignore GitLab vulnerabilities
        // that do not report a usable CWE identifier.
        if (reportedCwe is not > 0)
        {
            return null;
        }

        var id = StringValue(vulnerability["id"]);
        var name = StringValue(vulnerability["name"]);
        var description = BuildDescription(vulnerability);
        var severity = NormalizeSeverity(
            StringValue(vulnerability["severity"]));

        var scannerId = StringValue(
            vulnerability["scanner"]?["id"]);

        var scannerName = StringValue(
            vulnerability["scanner"]?["name"]);

        var filePath = NormalizePath(
            StringValue(vulnerability["location"]?["file"]));

        var startLine = IntValue(
            vulnerability["location"]?["start_line"]);

        var endLine = IntValue(
            vulnerability["location"]?["end_line"]);

        var reference = FindReference(vulnerability);

        var title = name
                    ?? $"GitLab SAST CWE-{reportedCwe.Value} finding";

        var finding = new CWEs(
            title: Truncate(title, 150),
            test: 3614,
            numericalSeverity: SeverityNumber(severity),
            foundBy: new List<int?> { 1 },
            severity: severity,
            description: description,
            staticFinding: true,
            dynamicFinding: false,
            filePath: filePath,
            line: startLine,
            references: reference);

        finding.Cwe = reportedCwe.Value;
        finding.Cve = ExtractCve(vulnerability);
        finding.VulnIdFromTool = id
                                 ?? scannerId
                                 ?? scannerName
                                 ?? $"CWE-{reportedCwe.Value}";

        finding.Date = ExtractDate(vulnerability);

        finding.Mitigation = BuildMitigation(
            vulnerability,
            endLine);

        return finding;
    }

    private static bool IsSastFinding(
        JObject vulnerability)
    {
        var category = StringValue(
            vulnerability["category"]);

        if (!string.IsNullOrWhiteSpace(category) &&
            !category.Equals(
                "sast",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var state = StringValue(
            vulnerability["state"]);

        if (state?.Equals(
                "dismissed",
                StringComparison.OrdinalIgnoreCase) == true)
        {
            return false;
        }

        return vulnerability["location"] is JObject ||
               vulnerability["identifiers"] is JArray;
    }

    private static int? ExtractReportedCwe(
        JObject vulnerability)
    {
        var identifiers = vulnerability["identifiers"]?
            .OfType<JObject>()
            .ToList()
            ?? new List<JObject>();

        // Highest-confidence source: an identifier explicitly typed as CWE.
        foreach (var identifier in identifiers)
        {
            var type = StringValue(identifier["type"]);

            if (!type?.Equals(
                    "cwe",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                continue;
            }

            var cwe = ParseCweNumber(
                StringValue(identifier["value"]));

            cwe ??= ParseCweNumber(
                StringValue(identifier["name"]));

            cwe ??= ParseCweNumber(
                StringValue(identifier["url"]));

            if (cwe.HasValue)
            {
                return cwe;
            }
        }

        // Some producers identify the record as CWE without using exactly
        // "cwe" for the identifier type.
        foreach (var identifier in identifiers)
        {
            var cwe = ParseCweNumber(
                StringValue(identifier["name"]));

            cwe ??= ParseCweNumber(
                StringValue(identifier["value"]));

            cwe ??= ParseCweNumber(
                StringValue(identifier["url"]));

            if (cwe.HasValue)
            {
                return cwe;
            }
        }

        // Lower-confidence fallbacks for nonstandard GitLab analyzers.
        return ParseCweNumber(
                   StringValue(vulnerability["cwe"]))
               ?? ParseCweNumber(
                   StringValue(vulnerability["name"]))
               ?? ParseCweNumber(
                   StringValue(vulnerability["description"]));
    }

    private static int? ParseCweNumber(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // GitLab's CWE identifier value can be only the numeric portion,
        // such as "250".
        if (int.TryParse(
                value.Trim(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var numericCwe) &&
            numericCwe > 0)
        {
            return numericCwe;
        }

        var match = CweRegex.Match(value);

        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(
            match.Groups["id"].Value,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var parsedCwe)
            ? parsedCwe
            : null;
    }

    private static string BuildDescription(
        JObject vulnerability)
    {
        var parts = new List<string>();

        AddPart(
            parts,
            "Finding",
            StringValue(vulnerability["description"]));

        AddPart(
            parts,
            "Message",
            StringValue(vulnerability["message"]));

        AddPart(
            parts,
            "Solution",
            StringValue(vulnerability["solution"]));

        AddPart(
            parts,
            "Scanner",
            StringValue(vulnerability["scanner"]?["name"]));

        AddPart(
            parts,
            "Scanner ID",
            StringValue(vulnerability["scanner"]?["id"]));

        AddPart(
            parts,
            "GitLab vulnerability ID",
            StringValue(vulnerability["id"]));

        AddPart(
            parts,
            "Category",
            StringValue(vulnerability["category"]));

        AddPart(
            parts,
            "Confidence",
            StringValue(vulnerability["confidence"]));

        var locationDescription =
            BuildLocationDescription(vulnerability["location"] as JObject);

        AddPart(
            parts,
            "Location",
            locationDescription);

        var identifierDescription =
            BuildIdentifierDescription(vulnerability);

        AddPart(
            parts,
            "Identifiers",
            identifierDescription);

        return string.Join(
            Environment.NewLine,
            parts);
    }

    private static string? BuildLocationDescription(
        JObject? location)
    {
        if (location is null)
        {
            return null;
        }

        var file = StringValue(location["file"]);
        var startLine = IntValue(location["start_line"]);
        var endLine = IntValue(location["end_line"]);

        if (string.IsNullOrWhiteSpace(file) &&
            !startLine.HasValue &&
            !endLine.HasValue)
        {
            return null;
        }

        var locationText = file ?? "(file not reported)";

        if (startLine.HasValue)
        {
            locationText += $":{startLine.Value}";
        }

        if (endLine.HasValue &&
            endLine.Value != startLine)
        {
            locationText += $"-{endLine.Value}";
        }

        return locationText;
    }

    private static string? BuildIdentifierDescription(
        JObject vulnerability)
    {
        var descriptions = vulnerability["identifiers"]?
            .OfType<JObject>()
            .Select(identifier =>
            {
                var type = StringValue(identifier["type"]);
                var name = StringValue(identifier["name"]);
                var value = StringValue(identifier["value"]);

                if (!string.IsNullOrWhiteSpace(type) &&
                    !string.IsNullOrWhiteSpace(value))
                {
                    return $"{type}: {value}";
                }

                return name ?? value ?? type;
            })
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return descriptions is { Count: > 0 }
            ? string.Join(", ", descriptions)
            : null;
    }

    private static string? FindReference(
        JObject vulnerability)
    {
        var identifiers = vulnerability["identifiers"]?
            .OfType<JObject>()
            .ToList()
            ?? new List<JObject>();

        // Prefer the reported CWE reference.
        var cweReference = identifiers
            .FirstOrDefault(identifier =>
                StringValue(identifier["type"])?.Equals(
                    "cwe",
                    StringComparison.OrdinalIgnoreCase) == true);

        var reference = StringValue(
            cweReference?["url"]);

        if (!string.IsNullOrWhiteSpace(reference))
        {
            return reference;
        }

        // Otherwise retain the first analyzer-supplied reference.
        return identifiers
            .Select(identifier =>
                StringValue(identifier["url"]))
            .FirstOrDefault(url =>
                !string.IsNullOrWhiteSpace(url));
    }

    private static string? ExtractCve(
        JObject vulnerability)
    {
        foreach (var identifier in vulnerability["identifiers"]?
                     .OfType<JObject>()
                 ?? Enumerable.Empty<JObject>())
        {
            var type = StringValue(identifier["type"]);

            if (type?.Equals(
                    "cve",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                var directValue = StringValue(identifier["value"])
                                  ?? StringValue(identifier["name"]);

                var directMatch = FindFirstCve(directValue);

                if (directMatch is not null)
                {
                    return directMatch;
                }
            }

            var identifierMatch = FindFirstCve(
                StringValue(identifier["value"]),
                StringValue(identifier["name"]),
                StringValue(identifier["url"]));

            if (identifierMatch is not null)
            {
                return identifierMatch;
            }
        }

        return FindFirstCve(
            StringValue(vulnerability["cve"]),
            StringValue(vulnerability["name"]),
            StringValue(vulnerability["description"]));
    }

    private static DateTime? ExtractDate(
        JObject vulnerability)
    {
        var candidateValues = new[]
        {
            StringValue(vulnerability["updated_at"]),
            StringValue(vulnerability["created_at"]),
            StringValue(vulnerability["detected_at"]),
            StringValue(vulnerability["first_detected_at"]),
            StringValue(vulnerability["last_detected_at"])
        };

        foreach (var value in candidateValues.Where(
                     value => !string.IsNullOrWhiteSpace(value)))
        {
            if (DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal |
                    DateTimeStyles.AdjustToUniversal,
                    out var parsedDate))
            {
                return parsedDate;
            }
        }

        return null;
    }

    private static string? BuildMitigation(
        JObject vulnerability,
        int? endLine)
    {
        var parts = new List<string>();

        AddPlainPart(
            parts,
            StringValue(vulnerability["solution"]));

        AddPlainPart(
            parts,
            StringValue(vulnerability["remediation"]));

        if (endLine.HasValue)
        {
            AddPlainPart(
                parts,
                $"Finding ends at line {endLine.Value}.");
        }

        return parts.Count > 0
            ? string.Join(Environment.NewLine, parts)
            : null;
    }

    private static string NormalizeSeverity(
        string? severity)
    {
        return severity?.Trim().ToLowerInvariant() switch
        {
            "critical" => "Critical",
            "high" => "High",
            "medium" => "Medium",
            "moderate" => "Medium",
            "low" => "Low",
            "info" => "Info",
            "informational" => "Info",
            "unknown" => "Info",
            _ => "Medium"
        };
    }

    private static string SeverityNumber(
        string severity)
    {
        return severity switch
        {
            "Critical" => "100",
            "High" => "75",
            "Medium" => "50",
            "Low" => "25",
            _ => "0"
        };
    }

    private static string? NormalizePath(
        string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        return filePath
            .Replace('\\', '/')
            .TrimStart('/');
    }

    private static void AddPart(
        ICollection<string> parts,
        string label,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalized = value.Trim();

        if (parts.Any(existing =>
                existing.EndsWith(
                    normalized,
                    StringComparison.Ordinal)))
        {
            return;
        }

        parts.Add($"**{label}:** {normalized}");
    }

    private static void AddPlainPart(
        ICollection<string> parts,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalized = value.Trim();

        if (!parts.Contains(
                normalized,
                StringComparer.Ordinal))
        {
            parts.Add(normalized);
        }
    }

    private static string? FindFirstCve(
        params string?[] values)
    {
        foreach (var value in values.Where(
                     value => !string.IsNullOrWhiteSpace(value)))
        {
            var match = CveRegex.Match(value!);

            if (match.Success)
            {
                return match.Value.ToUpperInvariant();
            }
        }

        return null;
    }

    private static string Truncate(
        string value,
        int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }

    private static string? StringValue(
        JToken? token)
    {
        if (token is null ||
            token.Type is JTokenType.Null or JTokenType.Undefined)
        {
            return null;
        }

        if (token.Type == JTokenType.Array)
        {
            return string.Join(
                " ",
                token.Values<string>());
        }

        return token.Type == JTokenType.String
            ? token.Value<string>()
            : token.ToString(Formatting.None);
    }

    private static int? IntValue(
        JToken? token)
    {
        if (token is null ||
            token.Type is JTokenType.Null or JTokenType.Undefined)
        {
            return null;
        }

        if (token.Type == JTokenType.Integer)
        {
            return token.Value<int?>();
        }

        return int.TryParse(
            token.ToString(),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : null;
    }

    private static async Task SkipValueAsync(
        JsonTextReader reader)
    {
        if (reader.TokenType is not (
                JsonToken.StartObject or
                JsonToken.StartArray))
        {
            return;
        }

        var startingDepth = reader.Depth;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            if (reader.Depth == startingDepth &&
                reader.TokenType is (
                    JsonToken.EndObject or
                    JsonToken.EndArray))
            {
                return;
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}