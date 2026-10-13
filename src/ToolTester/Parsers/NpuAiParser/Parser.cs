using global::ToolTester.Application.Common.Interfaces;
using global::ToolTester.Application.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ToolTester.Parsers.OpenVINO;

/// <summary>
/// Parser for newline-delimited JSON produced by the OpenVINO NPU
/// static-analysis scanner.
///
/// Each non-empty line must contain one complete JSON object:
///
/// {
///   "sourceFile": "...",
///   "relativePath": "...",
///   "status": "Completed",
///   "scannedUtc": "...",
///   "findings": [ ... ],
///   "errors": [ ... ]
/// }
///
/// Completed and Partial records are imported. Failed records are skipped.
/// Partial records may still contain valid findings from successful chunks.
/// </summary>
public sealed class Parser : IParser
{
    private const int DefaultTestId = 3614;
    private const int DefaultScannerId = 1;

    private static readonly Regex CweRegex = new(
        @"(?i)\bCWE[-_:/ ]?(?<id>\d+)\b",
        RegexOptions.Compiled |
        RegexOptions.CultureInvariant);

    private static readonly Regex CveRegex = new(
        @"(?i)\bCVE-\d{4}-\d{4,}\b",
        RegexOptions.Compiled |
        RegexOptions.CultureInvariant);

    private static readonly Regex FirstIntegerRegex = new(
        @"(?<!\d)(?<number>\d+)(?!\d)",
        RegexOptions.Compiled |
        RegexOptions.CultureInvariant);

    private bool _disposed;

    public object get_scan_types() =>
        new List<object>
        {
            "OpenVINO NPU JSONL"
        };

    public object get_label_for_scan_types(
        object scan_type) =>
        scan_type;

    public object get_description_for_scan_types(
        object scan_type) =>
        "OpenVINO NPU SAST newline-delimited JSON reports can be imported.";

    /// <summary>
    /// Reads the JSONL stream one record at a time and returns fully
    /// populated ToolTester findings.
    ///
    /// This method does not load the entire JSONL document into memory,
    /// although the returned finding collection is accumulated in memory.
    /// </summary>
    public async Task<List<CWEs>> Get_findings(
        Stream stream)
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        ArgumentNullException.ThrowIfNull(stream);

        var findings = new List<CWEs>();

        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 64 * 1024,
            leaveOpen: true);

        string? line;
        long jsonLineNumber = 0;

        while ((line = await reader
                   .ReadLineAsync()
                   .ConfigureAwait(false)) != null)
        {
            jsonLineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            JObject fileRecord;

            try
            {
                fileRecord = JObject.Parse(line);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException(
                    $"Invalid OpenVINO NPU JSONL record " +
                    $"at line {jsonLineNumber:N0}. " +
                    "Each line must contain one complete JSON object.",
                    ex);
            }

            findings.AddRange(
                ParseFileRecord(
                    fileRecord,
                    jsonLineNumber));
        }

        return DeduplicateFindings(findings);
    }

    /// <summary>
    /// Compatibility implementation for the existing IParser contract.
    /// Returns the distinct CWE identifiers reported by the NPU scanner.
    /// </summary>
    List<int> IParser.Get_findings(
        Stream stream) =>
        Get_findings(stream)
            .GetAwaiter()
            .GetResult()
            .Select(finding =>
                finding.Cwe)
            .Where(cwe =>
                cwe is > 0)
            .Select(cwe =>
                cwe!.Value)
            .Distinct()
            .OrderBy(cwe =>
                cwe)
            .ToList();

    /// <summary>
    /// Parses one source-file record from one JSONL line.
    /// </summary>
    private static IEnumerable<CWEs> ParseFileRecord(
        JObject record,
        long jsonLineNumber)
    {
        var status =
            StringValue(record["status"]);

        // A Failed record has no reliable completed analysis.
        // Partial records are retained because successful chunks
        // may still contain useful findings.
        if (status?.Equals(
                "Failed",
                StringComparison.OrdinalIgnoreCase) == true)
        {
            yield break;
        }

        var sourceFile =
            StringValue(record["sourceFile"]);

        var relativePath =
            StringValue(record["relativePath"]);

        var scannedDate =
            DateValue(record["scannedUtc"]);

        var sourceCharacters =
            IntValue(record["sourceCharacters"]);

        var sourceLines =
            IntValue(record["sourceLines"]);

        var chunkCount =
            IntValue(record["chunkCount"]);

        var failedChunkCount =
            IntValue(record["failedChunkCount"]);

        var recordErrors =
            record["errors"]?
                .Values<string>()
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value))
                .ToList()
            ?? [];

        var findingTokens =
            record["findings"];

        if (findingTokens is null ||
            findingTokens.Type is
                JTokenType.Null or
                JTokenType.Undefined)
        {
            yield break;
        }

        if (findingTokens is not JArray findingsArray)
        {
            throw new InvalidDataException(
                $"The 'findings' property at JSONL line " +
                $"{jsonLineNumber:N0} is not an array.");
        }

        foreach (var result in
                 findingsArray.OfType<JObject>())
        {
            var finding = CreateFinding(
                result,
                sourceFile,
                relativePath,
                scannedDate,
                status,
                sourceCharacters,
                sourceLines,
                chunkCount,
                failedChunkCount,
                recordErrors);

            if (finding is not null)
            {
                yield return finding;
            }
        }
    }

    private static CWEs? CreateFinding(
        JObject result,
        string? sourceFile,
        string? relativePath,
        DateTime? scannedDate,
        string? status,
        int? sourceCharacters,
        int? sourceLines,
        int? chunkCount,
        int? failedChunkCount,
        IReadOnlyCollection<string> recordErrors)
    {
        // Preserve the CWE actually reported by the scanner.
        // Do not replace it with a CWE inferred from the Juliet path.
        var reportedCwe = ExtractReportedCwe(result);

        if (reportedCwe is not > 0)
        {
            return null;
        }

        var vulnerableLine =
            IntValue(result["line"]);

        var sourceLine =
            IntValue(result["sourceLine"]);

        var sinkLine =
            IntValue(result["sinkLine"]);

        // Prefer the scanner's primary line, then sink, then source.
        var effectiveLine = FirstPositiveValue(
            vulnerableLine,
            sinkLine,
            sourceLine);

        var confidence =
            ConfidenceValue(result["confidence"]);

        var rootCause =
            StringValue(result["rootCause"]);

        var explanation =
            StringValue(result["explanation"])
            ?? StringValue(result["reason"]);

        var title = BuildTitle(
            reportedCwe.Value,
            rootCause,
            explanation);

        var description = BuildDescription(
            result,
            status,
            relativePath,
            sourceLine,
            sinkLine,
            confidence,
            sourceCharacters,
            sourceLines,
            chunkCount,
            failedChunkCount,
            recordErrors);

        // Confidence and vulnerability severity are different concepts.
        // This report does not currently provide an impact severity, so
        // the parser uses Medium rather than treating confidence as impact.
        const string severity = "Medium";

        var toolFindingId =
            BuildToolFindingId(
                sourceFile ?? relativePath,
                reportedCwe.Value,
                effectiveLine,
                sourceLine,
                sinkLine);

        var finding = new CWEs(
            title: Truncate(title, 150),
            test: DefaultTestId,
            numericalSeverity:
                SeverityNumber(severity),
            foundBy:
            [
                DefaultScannerId
            ],
            severity: severity,
            description: description,
            staticFinding: true,
            dynamicFinding: false,
            filePath: sourceFile ?? relativePath,
            line: effectiveLine,
            references: null);

        finding.Cwe =
            reportedCwe.Value;

        finding.Date =
            scannedDate;

        finding.VulnIdFromTool =
            toolFindingId;

        finding.Cve = FindFirstCve(
            rootCause,
            explanation,
            StringValue(result["source"]),
            StringValue(result["sink"]));

        var mitigation =
            StringValue(result["mitigation"]);

        if (!string.IsNullOrWhiteSpace(mitigation))
        {
            finding.Mitigation =
                mitigation;
        }

        return finding;
    }

    /// <summary>
    /// Retrieves the CWE explicitly reported by the model.
    ///
    /// Supported examples:
    /// 121
    /// "121"
    /// "CWE-121"
    /// "CWE 121"
    /// </summary>
    private static int? ExtractReportedCwe(
        JObject result)
    {
        var directCwe = ExtractFirstCwe(
            result["cwe"],
            result["cweId"]);

        if (directCwe.HasValue)
        {
            return directCwe;
        }

        // These are fallbacks for model responses that accidentally place
        // the CWE in a textual field instead of the expected property.
        return ExtractFirstCwe(
            result["rootCause"],
            result["explanation"],
            result["reason"]);
    }

    private static int? ExtractFirstCwe(
        params JToken?[] tokens)
    {
        foreach (var token in tokens)
        {
            var cwe = ExtractCwe(token);

            if (cwe is > 0)
            {
                return cwe;
            }
        }

        return null;
    }

    private static int? ExtractCwe(
        JToken? token)
    {
        if (token is null ||
            token.Type is
                JTokenType.Null or
                JTokenType.Undefined)
        {
            return null;
        }

        if (token.Type ==
            JTokenType.Integer)
        {
            var value = token.Value<int>();

            return value > 0
                ? value
                : null;
        }

        if (token.Type ==
            JTokenType.Float)
        {
            var value = token.Value<double>();

            if (value <= 0 ||
                value > int.MaxValue)
            {
                return null;
            }

            return Convert.ToInt32(
                Math.Truncate(value));
        }

        if (token is JArray array)
        {
            foreach (var item in array)
            {
                var cwe = ExtractCwe(item);

                if (cwe is > 0)
                {
                    return cwe;
                }
            }

            return null;
        }

        var text =
            StringValue(token);

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (int.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var directCwe) &&
            directCwe > 0)
        {
            return directCwe;
        }

        var match =
            CweRegex.Match(text);

        if (match.Success &&
            int.TryParse(
                match.Groups["id"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var matchedCwe) &&
            matchedCwe > 0)
        {
            return matchedCwe;
        }

        return null;
    }

    private static string BuildTitle(
        int cwe,
        string? rootCause,
        string? explanation)
    {
        if (!string.IsNullOrWhiteSpace(rootCause))
        {
            return $"CWE-{cwe}: {rootCause.Trim()}";
        }

        if (!string.IsNullOrWhiteSpace(explanation))
        {
            return $"CWE-{cwe}: {explanation.Trim()}";
        }

        return $"OpenVINO NPU CWE-{cwe} finding";
    }

    private static string BuildDescription(
        JObject result,
        string? status,
        string? relativePath,
        int? sourceLine,
        int? sinkLine,
        double? confidence,
        int? sourceCharacters,
        int? sourceLines,
        int? chunkCount,
        int? failedChunkCount,
        IReadOnlyCollection<string> recordErrors)
    {
        var parts = new List<string>();

        AddPart(
            parts,
            "Scan status",
            status);

        AddPart(
            parts,
            "Relative path",
            relativePath);

        AddPart(
            parts,
            "Source",
            StringValue(result["source"]));

        AddIntegerPart(
            parts,
            "Source line",
            sourceLine);

        AddPart(
            parts,
            "Sink",
            StringValue(result["sink"]));

        AddIntegerPart(
            parts,
            "Sink line",
            sinkLine);

        AddPart(
            parts,
            "Data flow",
            StringValue(result["dataFlow"]));

        AddPart(
            parts,
            "Root cause",
            StringValue(result["rootCause"]));

        AddPart(
            parts,
            "Explanation",
            StringValue(result["explanation"])
            ?? StringValue(result["reason"]));

        if (confidence.HasValue)
        {
            AddPart(
                parts,
                "Model confidence",
                confidence.Value.ToString(
                    "P0",
                    CultureInfo.InvariantCulture));
        }

        AddIntegerPart(
            parts,
            "Source characters",
            sourceCharacters);

        AddIntegerPart(
            parts,
            "Source lines",
            sourceLines);

        AddIntegerPart(
            parts,
            "Chunk count",
            chunkCount);

        if (failedChunkCount is > 0)
        {
            AddIntegerPart(
                parts,
                "Failed chunks",
                failedChunkCount);
        }

        if (recordErrors.Count > 0)
        {
            AddPart(
                parts,
                "Scanner warnings",
                string.Join(
                    Environment.NewLine,
                    recordErrors));
        }

        return parts.Count > 0
            ? string.Join(
                Environment.NewLine,
                parts)
            : "OpenVINO NPU SAST finding.";
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

        var normalizedValue =
            value.Trim();

        var formatted =
            $"**{label}:** {normalizedValue}";

        if (!parts.Any(existing =>
                existing.Equals(
                    formatted,
                    StringComparison.Ordinal)))
        {
            parts.Add(formatted);
        }
    }

    private static void AddIntegerPart(
        ICollection<string> parts,
        string label,
        int? value)
    {
        if (!value.HasValue)
        {
            return;
        }

        AddPart(
            parts,
            label,
            value.Value.ToString(
                CultureInfo.InvariantCulture));
    }

    private static int? IntValue(
        JToken? token)
    {
        if (token is null ||
            token.Type is
                JTokenType.Null or
                JTokenType.Undefined)
        {
            return null;
        }

        if (token.Type ==
            JTokenType.Integer)
        {
            return token.Value<int?>();
        }

        if (token.Type ==
            JTokenType.Float)
        {
            var value = token.Value<double>();

            if (value < int.MinValue ||
                value > int.MaxValue)
            {
                return null;
            }

            return Convert.ToInt32(
                Math.Truncate(value));
        }

        var text =
            StringValue(token);

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (int.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var directValue))
        {
            return directValue;
        }

        var match =
            FirstIntegerRegex.Match(text);

        if (match.Success &&
            int.TryParse(
                match.Groups["number"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var extractedValue))
        {
            return extractedValue;
        }

        return null;
    }

    private static double? ConfidenceValue(
        JToken? token)
    {
        if (token is null ||
            token.Type is
                JTokenType.Null or
                JTokenType.Undefined)
        {
            return null;
        }

        double confidence;

        if (token.Type is
            JTokenType.Integer or
            JTokenType.Float)
        {
            confidence =
                token.Value<double>();
        }
        else
        {
            var text =
                StringValue(token);

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            text = text.Trim();

            var percentage =
                text.EndsWith(
                    "%",
                    StringComparison.Ordinal);

            if (percentage)
            {
                text = text[..^1].Trim();
            }

            if (!double.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out confidence))
            {
                return null;
            }

            if (percentage)
            {
                confidence /= 100.0;
            }
        }

        // Accept either 0.95 or 95.
        if (confidence > 1.0 &&
            confidence <= 100.0)
        {
            confidence /= 100.0;
        }

        return Math.Clamp(
            confidence,
            0.0,
            1.0);
    }

    private static int? FirstPositiveValue(
        params int?[] values)
    {
        foreach (var value in values)
        {
            if (value is > 0)
            {
                return value;
            }
        }

        return null;
    }

    private static DateTime? DateValue(
        JToken? token)
    {
        var raw =
            StringValue(token);

        return DateTime.TryParse(
            raw,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal |
            DateTimeStyles.AdjustToUniversal,
            out var date)
                ? date
                : null;
    }

    private static string BuildToolFindingId(
        string? sourceFile,
        int cwe,
        int? vulnerableLine,
        int? sourceLine,
        int? sinkLine)
    {
        var normalizedPath =
            string.IsNullOrWhiteSpace(sourceFile)
                ? "unknown-file"
                : sourceFile
                    .Replace('\\', '/')
                    .Trim();

        return string.Join(
            ":",
            "OpenVINO-NPU",
            $"CWE-{cwe}",
            vulnerableLine ?? 0,
            sourceLine ?? 0,
            sinkLine ?? 0,
            normalizedPath);
    }

    private static string SeverityNumber(
        string severity) =>
        severity switch
        {
            "Critical" => "100",
            "High" => "75",
            "Medium" => "50",
            "Low" => "25",
            _ => "0"
        };

    private static string? FindFirstCve(
        params string?[] values)
    {
        foreach (var value in
                 values.Where(value =>
                     !string.IsNullOrWhiteSpace(value)))
        {
            var match =
                CveRegex.Match(value!);

            if (match.Success)
            {
                return match.Value
                    .ToUpperInvariant();
            }
        }

        return null;
    }

    private static List<CWEs> DeduplicateFindings(
        IEnumerable<CWEs> findings)
    {
        return findings
            .Where(finding =>
                finding.Cwe is > 0)
            .GroupBy(finding => new
            {
                Cwe = finding.Cwe!.Value,

                FilePath =
                    NormalizeFilePath(
                        finding.FilePath),

                Line =
                    finding.Line ?? 0,

                ToolFindingId =
                    finding.VulnIdFromTool ??
                    string.Empty
            })
            .Select(group =>
                group.First())
            .OrderBy(finding =>
                finding.FilePath)
            .ThenBy(finding =>
                finding.Line)
            .ThenBy(finding =>
                finding.Cwe)
            .ToList();
    }

    private static string NormalizeFilePath(
        string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return path
            .Replace('\\', '/')
            .Trim()
            .ToUpperInvariant();
    }

    private static string? StringValue(
        JToken? token)
    {
        if (token is null ||
            token.Type is
                JTokenType.Null or
                JTokenType.Undefined)
        {
            return null;
        }

        if (token.Type ==
            JTokenType.Array)
        {
            return string.Join(
                " ",
                token
                    .Children()
                    .Select(StringValue)
                    .Where(value =>
                        !string.IsNullOrWhiteSpace(value)));
        }

        if (token.Type ==
            JTokenType.String)
        {
            return token.Value<string>();
        }

        return token.ToString(
            Formatting.None);
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
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}