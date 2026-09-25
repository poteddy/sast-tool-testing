using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;
using ToolTester.Parsers.CPPChecker.Models;

namespace ToolTester.Parsers.CPPChecker;

/// <summary>Parses Cppcheck XML report format version 2.</summary>
public sealed partial class Parser : IParser
{
    private bool _disposed;

    public IReadOnlyList<string> GetScanTypes() => ["Cppcheck XML"];

    public string GetLabelForScanType(string scanType) => scanType;

    public string GetDescriptionForScanType(string scanType) =>
        "Cppcheck XML report files generated with --xml-version=2.";

    // Compatibility wrapper for the naming used by the supplied SARIF parser.
    public Task<List<CWEs>> Get_findings(Stream fs) => GetFindingsAsync(fs);
    List<int> IParser.Get_findings(Stream fs)
    {
        throw new NotImplementedException();
    }


    public async Task<List<CWEs>> GetFindingsAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(stream);

        var findings = new List<CWEs>();
        var settings = new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreWhitespace = true,
            CloseInput = false
        };

        using var reader = XmlReader.Create(stream, settings);
        while (await reader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "error")
            {
                continue;
            }

            using var subtree = reader.ReadSubtree();
            var error = await XElement.LoadAsync(subtree, LoadOptions.None, cancellationToken);
            findings.Add(ParseError(error));
        }

        return findings;
    }

    private static CWEs ParseError(XElement error)
    {
        var id = GetDecodedAttribute(error, "id");
        var cppcheckSeverity = GetDecodedAttribute(error, "severity");
        var message = GetDecodedAttribute(error, "msg");
        var verbose = GetDecodedAttribute(error, "verbose");
        var file0 = NormalizePath(GetDecodedAttribute(error, "file0"));
        var cwe = ParseNullableInt(GetDecodedAttribute(error, "cwe"));
        var sinceDate = ParseDate(GetDecodedAttribute(error, "sinceDate"));
        var inconclusive = ParseBoolean(GetDecodedAttribute(error, "inconclusive"));

        var locations = error.Elements("location")
            .Select(ParseLocation)
            .ToList();

        var primary = locations.FirstOrDefault();
        var filePath = primary?.FilePath ?? file0;
        var title = Truncate(
            FirstNonEmpty(message, verbose, id, "Cppcheck finding"),
            150);

        var result = new CWEs(title: title
            , test: 4
            , numericalSeverity: "100"
            , foundBy: new List<int?>() { 1 }
            , severity: MapSeverity(cppcheckSeverity)
            , description: BuildDescription(message, verbose, id, cppcheckSeverity, locations)
            , staticFinding: true
            , dynamicFinding: false
            , filePath: filePath
            , line: primary?.Line
            , references: ""
            );

        result.Cve = TryExtractCve(id) ?? TryExtractCve(message) ?? TryExtractCve(verbose);
        result.Cwe = cwe.Value;
        result.Date = DateTime.Parse(sinceDate.Value.ToUniversalTime().ToString());

        return result;                             
    }

    private static CppcheckLocation ParseLocation(XElement location) => new(
        NormalizePath(GetDecodedAttribute(location, "file")),
        ParseNullableInt(GetDecodedAttribute(location, "line")),
        ParseNullableInt(GetDecodedAttribute(location, "column")),
        NullIfWhiteSpace(GetDecodedAttribute(location, "info")));

    private static string BuildDescription(
        string? message,
        string? verbose,
        string? id,
        string? cppcheckSeverity,
        IReadOnlyList<CppcheckLocation> locations)
    {
        var builder = new StringBuilder();

        AppendLine(builder, "Result message", message);
        if (!string.Equals(verbose, message, StringComparison.Ordinal))
        {
            AppendLine(builder, "Details", verbose);
        }
        AppendLine(builder, "Cppcheck rule", id);
        AppendLine(builder, "Cppcheck severity", cppcheckSeverity);

        var traceLocations = locations.Where(location => !string.IsNullOrWhiteSpace(location.Info)).ToList();
        if (traceLocations.Count > 0)
        {
            builder.AppendLine("**Trace:**");
            foreach (var location in traceLocations)
            {
                var position = location.Line is null
                    ? location.FilePath
                    : $"{location.FilePath}:{location.Line}";
                builder.Append("- ").Append(position ?? "Unknown location")
                    .Append(": ").AppendLine(location.Info);
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendLine(StringBuilder builder, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.Append("**").Append(label).Append(":** ").AppendLine(value);
        }
    }

    private static string MapSeverity(string? severity) => severity?.ToLowerInvariant() switch
    {
        "error" => "Critical",
        "warning" => "Medium",
        "style" => "Low",
        "performance" => "Low",
        "portability" => "Low",
        "information" => "Info",
        "debug" => "Info",
        _ => "Medium"
    };

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var formats = new[] { "M/d/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "O" };
        return DateTimeOffset.TryParseExact(
            value,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var parsed)
            ? parsed
            : DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed)
                ? parsed
                : null;
    }

    private static int? ParseNullableInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static bool ParseBoolean(string? value) =>
        bool.TryParse(value, out var parsed) && parsed;

    private static string? NormalizePath(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Replace('\\', '/');

    private static string? GetDecodedAttribute(XElement element, string name)
    {
        var value = element.Attribute(name)?.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // The supplied report contains values such as &amp;#039;, which require
        // a second HTML decode after the XML parser resolves &amp;.
        var decoded = value;
        for (var i = 0; i < 2; i++)
        {
            var next = WebUtility.HtmlDecode(decoded);
            if (next == decoded)
            {
                break;
            }
            decoded = next;
        }

        return decoded.Trim();
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.First(value => !string.IsNullOrWhiteSpace(value))!;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static string? TryExtractCve(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

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
