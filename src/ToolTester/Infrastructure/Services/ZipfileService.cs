using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;

namespace ToolTester.Infrastructure.Services;

public sealed class ZipfileService : IZipfileService
{
    private static readonly Regex CweFolderRegex = new(
        @"testcases/(CWE(?=\D*)(\d+)[^/]*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex JulietRegex = new(
        @"CWE(\d+).*__CWE(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Dictionary<int, int> FileCount(string zipFileName)
    {
        return GetTestCases(zipFileName)
            .GroupBy(x => x.FolderCweId)
            .ToDictionary(
                g => g.Key,
                g => g.Count());
    }

    public List<ZipTestCaseInfo> GetTestCases(string zipFileName)
    {
        var results = new List<ZipTestCaseInfo>();

        using var archive = ZipFile.OpenRead(zipFileName);

        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith("/"))
            {
                continue;
            }

            var match = CweFolderRegex.Match(entry.FullName);

            if (!match.Success)
            {
                continue;
            }

            results.Add(new ZipTestCaseInfo(
                FolderCweId: int.Parse(match.Groups[2].Value),
                FolderName: match.Groups[1].Value,
                EntryName: entry.Name,
                FullPath: entry.FullName));
        }

        return results;
    }

    public Dictionary<int, string> GetCweNames(string zipFileName)
    {
        return GetTestCases(zipFileName)
            .GroupBy(x => x.FolderCweId)
            .ToDictionary(
                g => g.Key,
                g => g.First().FolderName);
    }

    public List<string> GetAllFileNames(string zipFileName)
    {
        return GetTestCases(zipFileName)
            .Select(x => x.FullPath)
            .ToList();
    }

    public IEnumerable<JulietCweMapping> GetJulietMappings(
        string zipFileName)
    {
        foreach (var testCase in GetTestCases(zipFileName))
        {
            var match = JulietRegex.Match(testCase.FullPath);

            if (!match.Success)
            {
                continue;
            }

            yield return new JulietCweMapping(
                PrimaryCweId: int.Parse(match.Groups[1].Value),
                SecondaryCweId: int.Parse(match.Groups[2].Value),
                FileName: testCase.FullPath);
        }
    }
}