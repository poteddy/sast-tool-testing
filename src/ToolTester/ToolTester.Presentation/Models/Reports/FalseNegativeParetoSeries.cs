namespace ToolTester.Presentation.Models.Reports;

public sealed class FalseNegativeParetoSeries
{
    public int ScanId { get; set; }
    public string ScanName { get; set; } = string.Empty;
    public string ScannerName { get; set; } = string.Empty;
    public List<FalseNegativeChartPoint> Items { get; set; } = [];
}
