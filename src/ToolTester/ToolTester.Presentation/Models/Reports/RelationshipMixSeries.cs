namespace ToolTester.Presentation.Models.Reports;

public sealed class RelationshipMixSeries
{
    public int ScanId { get; set; }
    public string ScanName { get; set; } = string.Empty;
    public List<RelationshipMixPoint> Items { get; set; } = [];
}
