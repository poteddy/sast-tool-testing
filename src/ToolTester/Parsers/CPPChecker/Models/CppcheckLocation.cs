namespace ToolTester.Parsers.CPPChecker.Models;

public sealed record CppcheckLocation(
    string? FilePath,
    int? Line,
    int? Column,
    string? Info);
