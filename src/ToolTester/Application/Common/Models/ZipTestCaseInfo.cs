namespace ToolTester.Application.Common.Models;

public sealed record ZipTestCaseInfo(
    int FolderCweId,
    string FolderName,
    string EntryName,
    string FullPath);