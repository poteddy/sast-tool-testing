using ToolTester.Presentation.Models;
using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Services
{
    public record ReportQueryResult(
        IReadOnlyList<RelatedSeries> RelatedSeries,
        IReadOnlyList<CweTestResults> Items,
        IReadOnlyList<TestSeries> TestSeries,
        IReadOnlyList<AggrigatedSeries> AggregatedSeries);
}