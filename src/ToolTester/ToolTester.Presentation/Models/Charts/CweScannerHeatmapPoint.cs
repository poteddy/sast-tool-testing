namespace ToolTester.Presentation.Models.Charts;

public sealed class CweScannerHeatmapPoint
{
    public int ScanId { get; init; }

    public string ScanName { get; init; } =
        string.Empty;

    public int ScannerIndex { get; init; }

    public int CweIndex { get; init; }

    public int CweId { get; init; }

    public string CweName { get; init; } =
        string.Empty;

    public int Opportunities { get; init; }

    public int Detected { get; init; }

    public int FalseNegatives { get; init; }

    public double DetectionRate { get; init; }
}