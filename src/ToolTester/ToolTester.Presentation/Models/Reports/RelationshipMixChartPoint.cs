namespace ToolTester.Presentation.Models.Reports;

public sealed class RelationshipMixChartPoint
{
    public int ScanId { get; set; }
    public string ScanName { get; set; } = string.Empty;
    public string ScannerName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public double Low { get; set; }
    public double High { get; set; }
}
