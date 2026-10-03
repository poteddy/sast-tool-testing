namespace ToolTester.Application.Reports.DTO;

public class RelatedSeries
{
    public int ScanId { get; set; }
    public string ScanName { get; set; } = string.Empty;
    public List<RelatedItemsInTest> Items { get; set; } = [];
}
