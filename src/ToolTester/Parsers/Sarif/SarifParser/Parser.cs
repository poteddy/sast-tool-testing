
using global::ToolTester.Application.Common.Interfaces;
using global::ToolTester.Application.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;

namespace ToolTester.Parsers.Sarif;

/// <summary>
/// Tolerant SARIF 2.1.x parser designed for CodeQL and other scanners.
/// It does not require result.kind == "fail", because SARIF permits kind to be omitted.
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

    public object get_scan_types() => new List<object> { "SARIF" };
    public object get_label_for_scan_types(object scan_type) => scan_type;
    public object get_description_for_scan_types(object scan_type) =>
        "SARIF 2.1.x report files, including CodeQL SARIF, can be imported.";

    /// <summary>Preferred asynchronous API. Returns fully populated findings.</summary>
    public async Task<List<CWEs>> Get_findings(Stream stream)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, leaveOpen: true);
        using var jsonReader = new JsonTextReader(reader)
        {
            DateParseHandling = DateParseHandling.None
        };

        var root = await JObject.LoadAsync(jsonReader).ConfigureAwait(false);
        var findings = new List<CWEs>();

        foreach (var run in root["runs"]?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
        {
            findings.AddRange(ParseRun(run));
        }

        return findings;
    }

    /// <summary>
    /// Compatibility implementation for the existing IParser contract shown in the
    /// original parser. It returns distinct CWE identifiers instead of throwing.
    /// Prefer Get_findings(Stream) when complete finding objects are required.
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
    private static IEnumerable<CWEs> ParseRun(JObject run)
    {
        var rules = BuildRuleMap(run);
        var runDate = GetRunDate(run);

        foreach (var result in run["results"]?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
        {
            if (!IsFinding(result))
            {
                continue;
            }

            var ruleId = StringValue(result["ruleId"])
                         ?? StringValue(result["rule"]?["id"])
                         ?? StringValue(result["descriptor"]?["id"]);

            JObject? rule = null;
            if (!string.IsNullOrWhiteSpace(ruleId))
            {
                rules.TryGetValue(ruleId, out rule);
            }

            if (rule is null && IntValue(result["ruleIndex"]) is int ruleIndex)
            {
                rule = Rules(run).ElementAtOrDefault(ruleIndex);
                ruleId ??= StringValue(rule?["id"]);
            }

            yield return CreateFinding(result, rule, ruleId, runDate);
        }
    }

    private static bool IsFinding(JObject result)
    {
        var kind = StringValue(result["kind"]);
        if (kind is not null &&
            (kind.Equals("pass", StringComparison.OrdinalIgnoreCase) ||
             kind.Equals("notApplicable", StringComparison.OrdinalIgnoreCase) ||
             kind.Equals("informational", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var ruleId = StringValue(result["ruleId"])
                     ?? StringValue(result["rule"]?["id"])
                     ?? StringValue(result["descriptor"]?["id"]);

        // CodeQL places extraction/baseline telemetry in SARIF-like result collections.
        if (ruleId?.Contains("/diagnostics/", StringComparison.OrdinalIgnoreCase) == true ||
            ruleId?.Contains("/baseline/", StringComparison.OrdinalIgnoreCase) == true ||
            ruleId?.StartsWith("cli/", StringComparison.OrdinalIgnoreCase) == true)
        {
            return false;
        }

        var level = StringValue(result["level"]);
        if (level?.Equals("none", StringComparison.OrdinalIgnoreCase) == true &&
            string.IsNullOrWhiteSpace(StringValue(result["message"]?["text"])))
        {
            return false;
        }

        // A normal SARIF finding generally has a rule identity or a meaningful message.
        return !string.IsNullOrWhiteSpace(ruleId) ||
               !string.IsNullOrWhiteSpace(StringValue(result["message"]?["text"]));
    }

    private static CWEs CreateFinding(
        JObject result,
        JObject? rule,
        string? ruleId,
        DateTime? runDate)
    {
        var location = result["locations"]?
            .OfType<JObject>()
            .FirstOrDefault();

        var physical = location?["physicalLocation"];

        var filePath = StringValue(
            physical?["artifactLocation"]?["uri"]);

        var line = IntValue(
            physical?["region"]?["startLine"]);

        var title = GetTitle(result, rule, ruleId);
        var description = GetDescription(result, rule);
        var severity = GetSeverity(result, rule);
        var reference = StringValue(rule?["helpUri"]);

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
            line: line,
            references: reference);

        finding.VulnIdFromTool = ruleId;
        finding.Cve = FindFirstCve(ruleId, title, description);
        finding.Date = runDate;

        // Store exactly what the scanner reported.
        var reportedCwe = ExtractReportedCwe(result, rule);

        if (reportedCwe.HasValue)
        {
            finding.Cwe = reportedCwe.Value;
        }

        var fixes = result["fixes"]?
            .OfType<JObject>()
            .Select(x => StringValue(x["description"]?["text"]))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (fixes?.Count > 0)
        {
            finding.Mitigation = string.Join(
                Environment.NewLine,
                fixes!);
        }

        return finding;
    }
    private static int? ExtractReportedCwe(JObject result, JObject? rule)
    {
        // Highest priority: CWE explicitly attached to this result.
        var resultCwe = ExtractFirstCwe(
            result["properties"]?["cwe"],
            result["properties"]?["CWE"]);

        if (resultCwe.HasValue)
        {
            return resultCwe;
        }

        // Next priority: CWE explicitly assigned to the referenced rule.
        var ruleCwe = ExtractFirstCwe(
            rule?["properties"]?["cwe"],
            rule?["properties"]?["CWE"]);

        if (ruleCwe.HasValue)
        {
            return ruleCwe;
        }

        // Some scanners place CWE identifiers in rule tags.
        foreach (var tag in rule?["properties"]?["tags"]?.Values<string>()
                            ?? Enumerable.Empty<string>())
        {
            var cwe = ExtractFirstCwe(tag);

            if (cwe.HasValue)
            {
                return cwe;
            }
        }

        // Some SARIF producers use rule relationships or taxonomy targets.
        foreach (var relationship in rule?["relationships"]?.OfType<JObject>()
                                     ?? Enumerable.Empty<JObject>())
        {
            var targetId = StringValue(relationship["target"]?["id"]);
            var cwe = ExtractFirstCwe(targetId);

            if (cwe.HasValue)
            {
                return cwe;
            }
        }

        // Lower-confidence fallbacks.
        return ExtractFirstCwe(
            rule?["id"],
            rule?["name"],
            rule?["shortDescription"]?["text"],
            rule?["fullDescription"]?["text"]);
    }

    private static int? ExtractFirstCwe(params JToken?[] tokens)
    {
        foreach (var token in tokens)
        {
            var value = StringValue(token);
            var cwe = ExtractFirstCwe(value);

            if (cwe.HasValue)
            {
                return cwe;
            }
        }

        return null;
    }

    private static int? ExtractFirstCwe(params string?[] values)
    {
        foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var match = CweRegex.Match(value!);

            if (match.Success &&
                int.TryParse(
                    match.Groups["id"].Value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var cwe))
            {
                return cwe;
            }
        }

        return null;
    }

    private static Dictionary<string, JObject> BuildRuleMap(JObject run) =>
        Rules(run)
            .Where(x => !string.IsNullOrWhiteSpace(StringValue(x["id"])))
            .GroupBy(x => StringValue(x["id"])!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<JObject> Rules(JObject run) =>
        run["tool"]?["driver"]?["rules"]?.OfType<JObject>()
        ?? Enumerable.Empty<JObject>();

    private static IEnumerable<int> ExtractCwes(JObject result, JObject? rule)
    {
        var candidates = new List<string?>
        {
            StringValue(result["properties"]?["cwe"]),
            StringValue(result["properties"]?["CWE"]),
            StringValue(rule?["properties"]?["cwe"]),
            StringValue(rule?["properties"]?["CWE"]),
            StringValue(rule?["id"]),
            StringValue(rule?["name"]),
            StringValue(rule?["shortDescription"]?["text"]),
            StringValue(rule?["fullDescription"]?["text"])
        };

        candidates.AddRange(rule?["properties"]?["tags"]?.Values<string>()
                            ?? Enumerable.Empty<string>());

        candidates.AddRange(rule?["relationships"]?
            .OfType<JObject>()
            .Select(x => StringValue(x["target"]?["id"]))
            ?? Enumerable.Empty<string?>());

        foreach (var candidate in candidates.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            foreach (Match match in CweRegex.Matches(candidate!))
            {
                if (int.TryParse(match.Groups["id"].Value, out var cwe))
                {
                    yield return cwe;
                }
            }
        }
    }

    private static string GetTitle(JObject result, JObject? rule, string? ruleId)
    {
        return StringValue(result["message"]?["text"])
               ?? StringValue(rule?["shortDescription"]?["text"])
               ?? StringValue(rule?["fullDescription"]?["text"])
               ?? StringValue(rule?["name"])
               ?? ruleId
               ?? "SARIF finding";
    }

    private static string GetDescription(JObject result, JObject? rule)
    {
        var parts = new List<string>();
        AddPart(parts, "Result message", StringValue(result["message"]?["text"]));
        AddPart(parts, "Snippet", StringValue(result["locations"]?[0]?["physicalLocation"]?["region"]?["snippet"]?["text"]));
        AddPart(parts, "Rule name", StringValue(rule?["name"]));
        AddPart(parts, "Rule short description", StringValue(rule?["shortDescription"]?["text"]));
        AddPart(parts, "Rule full description", StringValue(rule?["fullDescription"]?["text"]));
        return string.Join(Environment.NewLine, parts);
    }

    private static void AddPart(ICollection<string> parts, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            !parts.Any(x => x.EndsWith(value, StringComparison.Ordinal)))
        {
            parts.Add($"**{label}:** {value}");
        }
    }

    private static string GetSeverity(JObject result, JObject? rule)
    {
        var level = StringValue(result["level"])
                    ?? StringValue(rule?["defaultConfiguration"]?["level"]);

        return level?.ToLowerInvariant() switch
        {
            "error" => "Critical",
            "warning" => "Medium",
            "note" => "Info",
            "none" => "Info",
            _ => "Medium"
        };
    }

    private static string SeverityNumber(string severity) => severity switch
    {
        "Critical" => "100",
        "High" => "75",
        "Medium" => "50",
        "Low" => "25",
        _ => "0"
    };

    private static DateTime? GetRunDate(JObject run)
    {
        var raw = run["invocations"]?
            .OfType<JObject>()
            .Select(x => StringValue(x["endTimeUtc"]) ?? StringValue(x["startTimeUtc"]))
            .LastOrDefault(x => !string.IsNullOrWhiteSpace(x));

        return DateTime.TryParse(
            raw,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var date)
            ? date
            : null;
    }

    private static string? FindFirstCve(params string?[] values)
    {
        foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var match = CveRegex.Match(value!);
            if (match.Success)
            {
                return match.Value.ToUpperInvariant();
            }
        }

        return null;
    }

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private static string? StringValue(JToken? token)
    {
        if (token is null || token.Type is JTokenType.Null or JTokenType.Undefined)
        {
            return null;
        }

        if (token.Type == JTokenType.Array)
        {
            return string.Join(" ", token.Values<string>());
        }

        return token.Type == JTokenType.String ? token.Value<string>() : token.ToString(Formatting.None);
    }

    private static int? IntValue(JToken? token) => token?.Value<int?>();

    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
