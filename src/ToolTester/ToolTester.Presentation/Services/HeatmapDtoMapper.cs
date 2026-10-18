using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using ToolTester.Application.Reports.DTO;
using ToolTester.Presentation.Models.Charts;
namespace ToolTester.Presentation.Services
{
    // Presentation-only mapper: converts HeatmapDto -> HeatmapResult (LiveCharts types).
    public static class HeatmapDtoMapper
    {
        public static HeatmapResult ToHeatmapResult(this HeatmapDto dto)
        {
            var result = new HeatmapResult();
            if (dto == null || dto.Cells.Count == 0) return result;

            var scanIndexById = dto.Scans.Select((s, i) => new { s.ScanId, Index = i }).ToDictionary(x => x.ScanId, x => x.Index);
            var cweIndexById = dto.Cwes.Select((c, i) => new { c.CweId, Index = i }).ToDictionary(x => x.CweId, x => x.Index);

            var cells = dto.Cells
                .Select(c => new CweScannerHeatmapPoint
                {
                    ScanId = c.ScanId,
                    ScanName = dto.Scans.First(s => s.ScanId == c.ScanId).ScanName,
                    ScannerIndex = scanIndexById[c.ScanId],
                    CweIndex = cweIndexById[c.CweId],
                    CweId = c.CweId,
                    CweName = c.CweName ?? $"CWE-{c.CweId}",
                    Opportunities = c.Opportunities,
                    Detected = c.Detected,
                    FalseNegatives = c.FalseNegatives,
                    DetectionRate = c.DetectionRate
                })
                .OrderBy(x => x.CweIndex)
                .ThenBy(x => x.ScannerIndex)
                .ToList();

            var cellLookup = cells.ToDictionary(cell => $"{cell.ScannerIndex}:{cell.CweIndex}");

            var heatmapValues = cells
                .Select(cell => new WeightedPoint(cell.ScannerIndex, cell.CweIndex, cell.DetectionRate))
                .ToArray();

            result.Data = new System.Collections.ObjectModel.ObservableCollection<CweScannerHeatmapPoint>(cells);

            result.Series = new ISeries[]
            {
                new HeatSeries<WeightedPoint>
                {
                    Name = "Juliet detection rate",
                    Values = heatmapValues,
                    HeatMap = new []
                    {
                        new LvcColor(198,40,40),
                        new LvcColor(239,108,0),
                        new LvcColor(253,216,53),
                        new LvcColor(124,179,66),
                        new LvcColor(27,94,32)
                    },
                    YToolTipLabelFormatter = point =>
                    {
                        var scannerIndex = Convert.ToInt32(Math.Round(point.Coordinate.SecondaryValue));
                        var cweIndex = Convert.ToInt32(Math.Round(point.Coordinate.PrimaryValue));
                        var key = $"{scannerIndex}:{cweIndex}";
                        if (!cellLookup.TryGetValue(key, out var item)) return "No benchmark data";
                        return
                            $"Scanner: {item.ScanName}\n" +
                            $"Ground truth: CWE-{item.CweId}\n" +
                            $"CWE name: {item.CweName}\n" +
                            $"Opportunities: {item.Opportunities:N0}\n" +
                            $"Detected: {item.Detected:N0}\n" +
                            $"False negatives: {item.FalseNegatives:N0}\n" +
                            $"Detection rate: {item.DetectionRate:F2}%";
                    }
                }
            };

            result.XAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Scanner / Scan",
                    Labels = dto.Scans.Select(s => s.ScanName).ToArray(),
                    MinLimit = -0.5,
                    MaxLimit = dto.Scans.Count - 0.5,
                    MinStep = 1,
                    ForceStepToMin = true,
                    LabelsRotation = -45,
                    LabelsPaint = new SolidColorPaint(new SKColor(120,120,120)),
                    NamePaint = new SolidColorPaint(new SKColor(120,120,120)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(225,225,225)) { StrokeThickness = 1 },
                    Padding = new LiveChartsCore.Drawing.Padding(8)
                }
            };

            result.YAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Juliet Ground-Truth CWE",
                    Labels = dto.Cwes.Select(c => $"CWE-{c.CweId}").ToArray(),
                    MinLimit = -0.5,
                    MaxLimit = dto.Cwes.Count - 0.5,
                    MinStep = 1,
                    ForceStepToMin = true,
                    LabelsPaint = new SolidColorPaint(new SKColor(120,120,120)),
                    NamePaint = new SolidColorPaint(new SKColor(120,120,120)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(225,225,225)) { StrokeThickness = 1 },
                    Padding = new LiveChartsCore.Drawing.Padding(8)
                }
            };

            return result;
        }
    }
}