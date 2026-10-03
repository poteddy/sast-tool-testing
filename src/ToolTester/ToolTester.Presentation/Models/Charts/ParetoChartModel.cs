using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace ToolTester.Presentation.Models.Charts;

public sealed class ParetoChartModel
{
    public required string Title { get; init; }
    public required ISeries[] Series { get; init; }
    public required Axis[] XAxes { get; init; }
    public required Axis[] YAxes { get; init; }
}
