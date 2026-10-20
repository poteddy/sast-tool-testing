namespace ToolTester.Application.Reports.DTO;

public sealed class UnrelatedFindingConsensus
{
    public int ScannerFoundCweId { get; init; }

    public int GroundTruthCweId { get; init; }

    public int ScannerCount { get; init; }

    public int ScanCount { get; init; }

    public int FindingCount { get; init; }

    public IReadOnlyList<int> ScannerIds { get; init; } =
        [];

    public IReadOnlyList<string> ScannerNames { get; init; } =
        [];

    public IReadOnlyList<int> ScanIds { get; init; } =
        [];

    /*
     * Diagnostic examples used when reviewing whether an
     * Unrelated consensus pair needs a semantic rule.
     */
    public IReadOnlyList<ConsensusFindingExample>
        Examples
    { get; set; } = [];

    public string ScannerCweLabel =>
        $"CWE-{ScannerFoundCweId}";

    public string GroundTruthCweLabel =>
        $"CWE-{GroundTruthCweId}";

    public string ScannerNamesLabel =>
        string.Join(
            ", ",
            ScannerNames);
}

public sealed class ConsensusFindingExample
{
    public int ToolId { get; init; }

    public string ScannerName { get; init; } =
        string.Empty;

    public int ScanId { get; init; }

    public int TestId { get; init; }

    public string? FilePath { get; init; }

    public int? Line { get; init; }

    public string RuleId { get; init; } =
        string.Empty;

    public string Title { get; init; } =
        string.Empty;
}
