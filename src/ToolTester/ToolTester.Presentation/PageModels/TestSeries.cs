using ToolTester.Presentation.Models;

namespace ToolTester.Presentation.PageModels;

public class TestSeries
{
    public int ScanId { get; set; }
    public string ScanName { get; set; } = string.Empty;
    public List<CweTestResults> Items { get; set; } = [];
}
