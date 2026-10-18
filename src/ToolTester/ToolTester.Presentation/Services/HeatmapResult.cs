using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;
using ToolTester.Presentation.Models.Charts;

namespace ToolTester.Presentation.Services
{
    public sealed class HeatmapResult
    {
        public ObservableCollection<CweScannerHeatmapPoint> Data { get; set; } = new();
        public ISeries[] Series { get; set; } = System.Array.Empty<ISeries>();
        public Axis[] XAxes { get; set; } = System.Array.Empty<Axis>();
        public Axis[] YAxes { get; set; } = System.Array.Empty<Axis>();
        public bool HasData => Data != null && Data.Count > 0;
    }
}
