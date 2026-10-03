using ToolTester.Presentation.Models;

namespace ToolTester.Presentation.PageModels;

public class RelatedSeries
{
    public int ScanId { get; set; }
    public string ScanName { get; set; } = string.Empty;
    public List<RelatedItemsInTest> Items { get; set; } = [];
}
