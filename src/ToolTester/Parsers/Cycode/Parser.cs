using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;

// Change this namespace if CycodeViolationsResponse.cs is moved
// into the ToolTester solution.

namespace ToolTester.Parsers.Cycode;

/// <summary>
/// Parses Cycode violations API responses.
///
/// Supported root formats:
///
/// 1. Wrapped API response:
///    {
///        "items": [ ... ],
///        "next_page_token": "..."
///    }
///
/// 2. Raw violation array:
///    [ ... ]
/// </summary>
public sealed class Parser : IParser
{
    private static readonly Regex CweRegex = new(
        @"(?i)\bCWE[-_:/ ]?(?<id>\d+)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex CveRegex = new(
        @"(?i)\bCVE-\d{4}-\d{4,}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex HtmlTagRegex = new(
        @"<[^>]+>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly JsonSerializer JsonSerializer = JsonSerializer.Create(
        new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            DateParseHandling = DateParseHandling.DateTimeOffset
        });

    private bool _disposed;

    public object get_scan_types()
    {
        return new List<object>
        {
            "Cycode"
        };
    }

    public object get_label_for_scan_types(object scanType)
    {
        return scanType;
    }

    public object get_description_for_scan_types(object scanType)
    {
        return "Cycode violations API JSON files can be imported.";
    }

    /// <summary>
    /// Preferred asynchronous parser API.
    /// Returns fully populated ToolTester finding objects.
    /// </summary>
    public async Task<List<CWEs>> Get_findings(Stream stream)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(stream);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var streamReader = new StreamReader(
            stream,
            leaveOpen: true);

        using var jsonReader = new JsonTextReader(streamReader)
        {
            DateParseHandling = DateParseHandling.DateTimeOffset,
            CloseInput = false
        };

        var root = await JToken
            .LoadAsync(jsonReader)
            .ConfigureAwait(false);

        var violations = DeserializeViolations(root);

        return violations
            .Where(IsFinding)
            .Select(CreateFinding)
            .ToList();
    }

    /// <summary>
    /// Compatibility implementation for the existing IParser contract.
    /// Returns the distinct, positive CWE identifiers found in the report.
    /// </summary>
    List<int> IParser.Get_findings(Stream stream)
    {
        return Get_findings(stream)
            .GetAwaiter()
            .GetResult()
            .Select(f => f.Cwe)
            .Where(cwe => cwe is > 0)
            .Select(cwe => cwe!.Value)
            .Distinct()
            .ToList();
    }

    private static List<CycodeViolation> DeserializeViolations(
        JToken root)
    {
        switch (root.Type)
        {
            case JTokenType.Object:
                {
                    var response = root.ToObject<CycodeViolationsResponse>(
                        JsonSerializer);

                    return response?.Items ?? [];
                }

            case JTokenType.Array:
                {
                    return root.ToObject<List<CycodeViolation>>(
                               JsonSerializer)
                           ?? [];
                }

            case JTokenType.Null:
            case JTokenType.Undefined:
                return [];

            default:
                throw new JsonSerializationException(
                    $"Unsupported Cycode JSON root type '{root.Type}'. " +
                    "Expected an object containing 'items' or a raw JSON array.");
        }
    }

    private static bool IsFinding(
        CycodeViolation violation)
    {
        if (violation.IsHidden ||
            violation.IsPolicyDisabled)
        {
            return false;
        }

        // Keep informational findings. They are valid scanner results
        // and may still be relevant to scanner benchmark coverage.
        return
            !string.IsNullOrWhiteSpace(violation.SourcePolicyName) ||
            !string.IsNullOrWhiteSpace(violation.DetectionId) ||
            !string.IsNullOrWhiteSpace(
                violation.DetectionDetails?.FilePath);
    }

    private static CWEs CreateFinding(
        CycodeViolation violation)
    {
        var details = violation.DetectionDetails;

        var title = FirstNonEmpty(
            violation.SourcePolicyName,
            violation.CorrelationMessage,
            violation.DetectionId,
            "Cycode finding");

        var description = BuildDescription(violation);

        var severity = NormalizeSeverity(
            violation.Severity,
            violation.RiskScoreSeverity,
            details?.ExternalSeverity);

        var reference = FirstNonEmpty(
            details?.FileUrl,
            details?.RepositoryUrl,
            details?.BranchUrl);

        var line = details?.LineInFile
                   ?? details?.Line
                   ?? details?.StartPosition;

        var finding = new CWEs(
            title: Truncate(CleanText(title), 150),
            test: 3614,
            numericalSeverity: SeverityNumber(severity),
            foundBy: new List<int?> { 1 },
            severity: severity,
            description: description,
            staticFinding: true,
            dynamicFinding: false,
            filePath: details?.FilePath,
            line: line,
            references: CleanUrl(reference));

        finding.VulnIdFromTool = GetToolVulnerabilityId(
            violation);

        finding.Cve = FindFirstCve(
            violation.SourcePolicyName,
            violation.CorrelationMessage,
            violation.DetectionId,
            violation.DetectionTypeId?.ToString(),
            details?.FilePath,
            JoinValues(details?.Cwe),
            JoinValues(details?.Owasp),
            JoinValues(violation.Labels),
            JoinValues(violation.PolicyLabels));

        finding.Date = GetFindingDate(violation);

        var reportedCwe = ExtractReportedCwe(violation);

        if (reportedCwe.HasValue)
        {
            finding.Cwe = reportedCwe.Value;
        }
     

        finding.Mitigation = BuildMitigation(violation);

        return finding;
    }

    private static string GetToolVulnerabilityId(
        CycodeViolation violation)
    {
        // DetectionRuleId is normally the most useful stable scanner-rule
        // identifier for mapping findings back to the scanner rule.
        return FirstNonEmpty(
            violation.DetectionRuleId?.ToString(),
            violation.DetectionTypeId?.ToString(),
            violation.DetectionId,
            violation.Id?.ToString(),
            violation.SourcePolicyName);
    }

    private static int? ExtractReportedCwe(
        CycodeViolation violation)
    {
        var details = violation.DetectionDetails;

        // Highest priority: the explicit Cycode CWE collection.
        var explicitCwe = ExtractFirstCwe(
            details?.Cwe?.ToArray() ?? []);

        if (explicitCwe.HasValue)
        {
            return explicitCwe;
        }

        // Lower-confidence fallbacks for responses where Cycode did not
        // populate detection_details.cwe.
        return ExtractFirstCwe(
            violation.SourcePolicyName,
            violation.CorrelationMessage,
            violation.DetectionId,
            JoinValues(violation.PolicyLabels),
            JoinValues(violation.Labels),
            details?.Category,
            details?.FilePath,
            details?.FileName);
    }

    private static int? ExtractFirstCwe(
        params string?[] values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var match = CweRegex.Match(value);

            if (!match.Success)
            {
                continue;
            }

            if (int.TryParse(
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

    private static string BuildDescription(
        CycodeViolation violation)
    {
        var details = violation.DetectionDetails;
        var parts = new List<string>();

        AddPart(
            parts,
            "Policy",
            violation.SourcePolicyName);

        AddPart(
            parts,
            "Description",
            violation.CorrelationMessage);

        AddPart(
            parts,
            "Status",
            violation.Status);

        AddPart(
            parts,
            "Provider",
            violation.Provider);

        AddPart(
            parts,
            "Source type",
            violation.SourcePolicyType);

        AddPart(
            parts,
            "Repository",
            details?.RepositoryName);

        AddPart(
            parts,
            "Branch",
            details?.BranchName);

        AddPart(
            parts,
            "File",
            details?.FilePath);

        AddPart(
            parts,
            "Line",
            GetLine(details)?.ToString(
                CultureInfo.InvariantCulture));

        AddPart(
            parts,
            "Category",
            FirstNonEmpty(
                violation.SubCategoryV2,
                violation.SubCategory,
                details?.Category));

        AddPart(
            parts,
            "CWE",
            JoinValues(details?.Cwe));

        AddPart(
            parts,
            "OWASP",
            JoinValues(details?.Owasp));

        AddPart(
            parts,
            "Languages",
            JoinValues(details?.Languages));

        AddPart(
            parts,
            "Risk score",
            violation.RiskScore > 0
                ? violation.RiskScore.ToString(
                    "0.##",
                    CultureInfo.InvariantCulture)
                : null);

        AddPart(
            parts,
            "Risk severity",
            violation.RiskScoreSeverity);

        AddPart(
            parts,
            "Detection ID",
            violation.DetectionId);

        return string.Join(
            Environment.NewLine,
            parts);
    }

    private static int? GetLine(
        CycodeDetectionDetails? details)
    {
        return details?.LineInFile
               ?? details?.Line
               ?? details?.StartPosition;
    }

    private static string? BuildMitigation(
        CycodeViolation violation)
    {
        if (violation.Remediations.Count == 0 &&
            violation.DetectionDetails?.RemediationDetails is null &&
            violation.DetectionDetails?.RemediationActions is null)
        {
            return null;
        }

        var parts = new List<string>();

        foreach (var remediation in violation.Remediations)
        {
            var text = ExtractTokenText(remediation);

            if (!string.IsNullOrWhiteSpace(text))
            {
                parts.Add(CleanText(text));
            }
        }

        var remediationDetails = ExtractTokenText(
            violation.DetectionDetails?.RemediationDetails);

        if (!string.IsNullOrWhiteSpace(remediationDetails))
        {
            parts.Add(CleanText(remediationDetails));
        }

        var remediationActions = ExtractTokenText(
            violation.DetectionDetails?.RemediationActions);

        if (!string.IsNullOrWhiteSpace(remediationActions))
        {
            parts.Add(CleanText(remediationActions));
        }

        var distinctParts = parts
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return distinctParts.Count == 0
            ? null
            : string.Join(
                Environment.NewLine,
                distinctParts);
    }

    private static string? ExtractTokenText(
        JToken? token)
    {
        if (token is null ||
            token.Type is JTokenType.Null or JTokenType.Undefined)
        {
            return null;
        }

        if (token.Type == JTokenType.String)
        {
            return token.Value<string>();
        }

        if (token.Type == JTokenType.Object)
        {
            return token["description"]?["text"]?.Value<string>()
                   ?? token["description"]?.Value<string>()
                   ?? token["message"]?["text"]?.Value<string>()
                   ?? token["message"]?.Value<string>()
                   ?? token["text"]?.Value<string>()
                   ?? token.ToString(Formatting.None);
        }

        return token.ToString(Formatting.None);
    }

    private static void AddPart(
        ICollection<string> parts,
        string label,
        string? value)
    {
        var cleanedValue = CleanText(value);

        if (string.IsNullOrWhiteSpace(cleanedValue))
        {
            return;
        }

        var formatted = $"**{label}:** {cleanedValue}";

        if (!parts.Contains(
                formatted,
                StringComparer.Ordinal))
        {
            parts.Add(formatted);
        }
    }

    private static string NormalizeSeverity(
        params string?[] severityValues)
    {
        var severity = severityValues.FirstOrDefault(
            value => !string.IsNullOrWhiteSpace(value));

        return severity?.Trim().ToLowerInvariant() switch
        {
            "critical" => "Critical",
            "very high" => "Critical",
            "error" => "Critical",

            "high" => "High",

            "medium" => "Medium",
            "moderate" => "Medium",
            "warning" => "Medium",

            "low" => "Low",

            "info" => "Info",
            "informational" => "Info",
            "note" => "Info",
            "none" => "Info",

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

    private static DateTime? GetFindingDate(
        CycodeViolation violation)
    {
        DateTimeOffset? date =
            violation.UpdatedDate != default
                ? violation.UpdatedDate
                : violation.CreatedDate != default
                    ? violation.CreatedDate
                    : violation.LastDetectedAt
                      ?? violation.StatusUpdatedAt;

        return date?.UtcDateTime;
    }

    private static string? FindFirstCve(
        params string?[] values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var match = CveRegex.Match(value);

            if (match.Success)
            {
                return match.Value.ToUpperInvariant();
            }
        }

        return null;
    }

    private static string FirstNonEmpty(
        params string?[] values)
    {
        return values.FirstOrDefault(
                   value => !string.IsNullOrWhiteSpace(value))
               ?? string.Empty;
    }

    private static string? JoinValues(
        IEnumerable<string>? values)
    {
        if (values is null)
        {
            return null;
        }

        var items = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return items.Count == 0
            ? null
            : string.Join(", ", items);
    }

    private static string CleanText(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decoded = WebUtility.HtmlDecode(value);
        var withoutTags = HtmlTagRegex.Replace(decoded, string.Empty);

        return withoutTags
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();
    }

    private static string? CleanUrl(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var decoded = WebUtility.HtmlDecode(value).Trim();

        // Normally the model receives a plain URL. This fallback handles
        // responses where a URL was accidentally wrapped in an HTML anchor.
        if (decoded.StartsWith(
                "<a ",
                StringComparison.OrdinalIgnoreCase))
        {
            var hrefMatch = Regex.Match(
                decoded,
                """href\s*=\s*?<url>[^"']+["']""",
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

            if (hrefMatch.Success)
            {
                return WebUtility.HtmlDecode(
                    hrefMatch.Groups["url"].Value);
            }
        }

        return decoded;
    }

    private static string Truncate(
        string value,
        int maximumLength)
    {
        if (string.IsNullOrEmpty(value) ||
            value.Length <= maximumLength)
        {
            return value;
        }

        return value[..maximumLength];
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}