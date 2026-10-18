using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
namespace ToolTester.Presentation.Services
{
    
    public sealed class FalsePositiveResult
    {
        public ISeries[] Series { get; set; } = System.Array.Empty<ISeries>();
        public Axis[] XAxes { get; set; } = System.Array.Empty<Axis>();
        public Axis[] YAxes { get; set; } = System.Array.Empty<Axis>();
        // light-weight legend info to convert to ChartLegendItem in VM
        public IReadOnlyList<(string Name, SKColor Color, bool IsVisible)> LegendInfo { get; set; } = new List<(string, SKColor, bool)>();
    }
}
