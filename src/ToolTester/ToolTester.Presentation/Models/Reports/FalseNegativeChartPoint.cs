namespace ToolTester.Presentation.Models.Reports;

public sealed class FalseNegativeChartPoint
{
    public int ScanId { get; set; }
    public string ScannerName { get; set; } = string.Empty;
    public int CweId { get; set; }
    public string CweName { get; set; } = string.Empty;
    public string CweLabel { get; set; } = string.Empty;
    public int Opportunities { get; set; }
    public int Detected { get; set; }
    public int FalseNegatives { get; set; }
    public double CumulativePercent { get; set; }
}
