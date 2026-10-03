namespace ToolTester.Presentation.Models.Charts;

public sealed class MitreTop25ChartPoint
{
    public MitreTop25ChartPoint()
    {
    }

    public int Rank { get; set; }

    public int CweId { get; set; }

    public string CweName { get; set; } =
        string.Empty;

    public int ScanId { get; set; }

    public string ScanName { get; set; } =
        string.Empty;

    public int Opportunities { get; set; }

    public int Detected { get; set; }

    public int FalseNegatives { get; set; }

    public double? DetectionRate { get; set; }
}
