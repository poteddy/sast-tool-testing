namespace ToolTester.Presentation.PageModels;

public class AggrigatedSeries
{
    public int ScanId { get; set; }
    public string ScanName { get; set; } = string.Empty;
    public List<AggrigatedItems> Items { get; set; } = [];
}
