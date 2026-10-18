using System.Text.RegularExpressions;
using ToolTester.Application.Common.Interfaces;

namespace ToolTester.Infrastructure.Services;

public sealed class JulietPathNormalizer : IJulietPathNormalizer
{
    private const string TestCasesSegment = "testcases/";
    private const string FilePrefix = "file:///";

    private static readonly Regex CwePathRegex = new(
        @"(?:^|[/\\])CWE[-_ ]?(?<number>\d+)(?:[/_\\]|$)",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled);

    public string Normalize(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return string.Empty;
        }

        var normalizedPath = filePath
            .Trim()
            .Trim('"')
            .Replace('\\', '/');

        /*
         * Convert:
         *
         * file:///C:/source/juliet/testcases/...
         *
         * to:
         *
         * C:/source/juliet/testcases/...
         */
        if (normalizedPath.StartsWith(
                FilePrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath =
                normalizedPath[FilePrefix.Length..];
        }

        /*
         * Collapse duplicate path separators.
         */
        while (normalizedPath.Contains(
                   "//",
                   StringComparison.Ordinal))
        {
            normalizedPath = normalizedPath.Replace(
                "//",
                "/",
                StringComparison.Ordinal);
        }

        /*
         * Remove environment-specific prefixes.
         *
         * Examples:
         *
         * C:/source/juliet/testcases/CWE121/...
         * /repo/juliet/testcases/CWE121/...
         * /workspace/testcases/CWE121/...
         *
         * all normalize to:
         *
         * testcases/cwe121/...
         */
        var testCasesIndex = normalizedPath.IndexOf(
            TestCasesSegment,
            StringComparison.OrdinalIgnoreCase);

        if (testCasesIndex >= 0)
        {
            normalizedPath =
                normalizedPath[testCasesIndex..];
        }

        /*
         * Canonical paths are lower case.
         *
         * This prevents Windows/Linux/scanner casing
         * differences from creating different Juliet tests.
         */
        return normalizedPath
            .TrimStart('/')
            .ToLowerInvariant();
    }

    public string GetFileName(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return string.Empty;
        }

        var normalizedPath =
            Normalize(filePath);

        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return string.Empty;
        }

        var separatorIndex =
            normalizedPath.LastIndexOf('/');

        if (separatorIndex >= 0)
        {
            return normalizedPath[(separatorIndex + 1)..];
        }

        return normalizedPath;
    }

    public int? ExtractCweId(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        var normalizedPath =
            Normalize(filePath);

        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return null;
        }

        var match =
            CwePathRegex.Match(normalizedPath);

        if (!match.Success)
        {
            return null;
        }

        var cweText =
            match.Groups["number"].Value;

        if (!int.TryParse(
                cweText,
                out var cweId))
        {
            return null;
        }

        return cweId;
    }

    public bool IsSupportedSourceFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var extension =
            Path.GetExtension(filePath);

        return
            extension.Equals(
                ".c",
                StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(
                ".cc",
                StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(
                ".cpp",
                StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(
                ".cxx",
                StringComparison.OrdinalIgnoreCase);
    }
}