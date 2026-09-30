using System.Collections.Generic;
using ToolTester.Application.Common.Models;

namespace ToolTester.Application.Common.Interfaces;

public interface IZipfileService
{
    Dictionary<int, int> FileCount(string zipFileName);

    List<ZipTestCaseInfo> GetTestCases(string zipFileName);

    Dictionary<int, string> GetCweNames(string zipFileName);

    List<string> GetAllFileNames(string zipFileName);

    IEnumerable<JulietCweMapping> GetJulietMappings(string zipFileName);
}