using System.IO.Compression;
using System.Text.RegularExpressions;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;

namespace ToolTester.Infrastructure.Services;

public sealed class ZipfileService : IZipfileService
{
    private static readonly Regex CweFolderRegex = new(
        @"testcases/(CWE(?=\D*)(\d+)[^/]*)",
        RegexOptions.Compiled |
        RegexOptions.IgnoreCase);

    private static readonly Regex JulietRegex = new(
        @"CWE(\d+).*__CWE(\d+)",
        RegexOptions.Compiled |
        RegexOptions.IgnoreCase);

    public Dictionary<int, int> FileCount(
        string zipFileName)
    {
        return GetTestCases(zipFileName)
            .GroupBy(testCase =>
                testCase.FolderCweId)
            .ToDictionary(
                group => group.Key,
                group => group.Count());
    }

    public List<ZipTestCaseInfo> GetTestCases(
        string zipFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            zipFileName);

        if (!File.Exists(zipFileName))
        {
            throw new FileNotFoundException(
                "The Juliet ZIP file was not found.",
                zipFileName);
        }

        var results =
            new List<ZipTestCaseInfo>();

        using var archive =
            ZipFile.OpenRead(zipFileName);

        foreach (var entry in archive.Entries)
        {
            /*
             * ZIP directory entries do not represent tests.
             */
            if (string.IsNullOrWhiteSpace(
                    entry.Name))
            {
                continue;
            }

            var normalizedFullName =
                entry.FullName.Replace(
                    '\\',
                    '/');

            var match =
                CweFolderRegex.Match(
                    normalizedFullName);

            if (!match.Success)
            {
                continue;
            }

            if (!int.TryParse(
                    match.Groups[2].Value,
                    out var folderCweId))
            {
                continue;
            }

            results.Add(
                new ZipTestCaseInfo(
                    FolderCweId:
                        folderCweId,

                    FolderName:
                        match.Groups[1].Value,

                    EntryName:
                        entry.Name,

                    FullPath:
                        normalizedFullName));
        }

        return results;
    }

    /*
     * Returns the individual Juliet paths needed when seeding
     * JulietTest.
     *
     * These are ZIP entry paths, not paths on the local machine.
     *
     * The IJulietPathNormalizer is responsible for converting
     * these values into canonical JulietTest.NormalizedPath values.
     */
    public IReadOnlyCollection<string> GetFilePaths(
        string zipFileName)
    {
        return GetTestCases(zipFileName)
            .Select(testCase =>
                testCase.FullPath)
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Dictionary<int, string> GetCweNames(
        string zipFileName)
    {
        return GetTestCases(zipFileName)
            .GroupBy(testCase =>
                testCase.FolderCweId)
            .ToDictionary(
                group => group.Key,
                group => group.First().FolderName);
    }

    /*
     * Retained for compatibility with existing callers.
     *
     * GetFilePaths should be used by the JulietTest seed.
     */
    public List<string> GetAllFileNames(
        string zipFileName)
    {
        return GetTestCases(zipFileName)
            .Select(testCase =>
                testCase.FullPath)
            .ToList();
    }

    public IEnumerable<JulietCweMapping>
        GetJulietMappings(
            string zipFileName)
    {
        foreach (var testCase in
                 GetTestCases(zipFileName))
        {
            var match =
                JulietRegex.Match(
                    testCase.FullPath);

            if (!match.Success)
            {
                continue;
            }

            if (!int.TryParse(
                    match.Groups[1].Value,
                    out var primaryCweId))
            {
                continue;
            }

            if (!int.TryParse(
                    match.Groups[2].Value,
                    out var secondaryCweId))
            {
                continue;
            }

            yield return new JulietCweMapping(
                PrimaryCweId:
                    primaryCweId,

                SecondaryCweId:
                    secondaryCweId,

                FileName:
                    testCase.FullPath);
        }
    }
}