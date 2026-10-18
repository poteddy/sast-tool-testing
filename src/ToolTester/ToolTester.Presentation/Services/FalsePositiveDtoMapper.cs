using global::ToolTester.Application.Reports.DTO;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
namespace ToolTester.Presentation.Services
{
    public static class FalsePositiveDtoMapper
        {
            private static readonly SKColor[] Palette = new[]
            {
            new SKColor(70,130,180),
            new SKColor(34,139,34),
            new SKColor(255,140,0),
            new SKColor(147,112,219),
            new SKColor(220,20,60),
            new SKColor(0,128,128),
            new SKColor(218,165,32),
            new SKColor(255,20,147)
        };

            public static FalsePositiveResult ToFalsePositiveResult(this FalsePositiveChartDto dto)
            {
                var result = new FalsePositiveResult();
                if (dto == null || dto.Series == null || dto.Series.Count == 0)
                    return result;

                var chartSeries = new List<ISeries>();
                var legendInfo = new List<(string Name, SKColor Color, bool IsVisible)>();
                var colorIndex = 0;

                foreach (var series in dto.Series.OrderBy(s => s.ScanId))
                {
                    if (series.Points == null || series.Points.Count == 0)
                        continue;

                    var color = Palette[(colorIndex++) % Palette.Length];
                    var name = series.ScanName ?? $"Scan {series.ScanId}";

                    var values = series.Points
                        .Select(p => new ObservablePoint(p.CweId, p.Count))
                        .ToArray();

                    var sc = new ScatterSeries<ObservablePoint>
                    {
                        Name = name,
                        Values = values,
                        IsVisible = true,
                        IsVisibleAtLegend = true,
                        GeometrySize = 12,
                        Fill = new SolidColorPaint(color),
                        Stroke = new SolidColorPaint(color) { StrokeThickness = 1 },
                        YToolTipLabelFormatter = point =>
                            $"Scan: {name}{Environment.NewLine}CWE: {point.Coordinate.SecondaryValue:F0}{Environment.NewLine}Count: {point.Coordinate.PrimaryValue:F0}"
                    };

                    chartSeries.Add(sc);
                    legendInfo.Add((name, color, true));
                }

                result.Series = chartSeries.ToArray();
                result.LegendInfo = legendInfo;

                result.XAxes = new[]
                {
                new Axis
                {
                    Name = "Scanner CWE",
                    Labeler = value => value.ToString("N0"),
                    MinStep = 1,
                    LabelsPaint = new SolidColorPaint(new SKColor(120,120,120)),
                    NamePaint = new SolidColorPaint(new SKColor(120,120,120)),
                    Padding = new LiveChartsCore.Drawing.Padding(6)
                }
            };

                result.YAxes = new[]
                {
                new Axis
                {
                    Name = "Finding Count",
                    MinLimit = 0,
                    Labeler = value => value.ToString("N0"),
                    LabelsPaint = new SolidColorPaint(new SKColor(120,120,120)),
                    NamePaint = new SolidColorPaint(new SKColor(120,120,120)),
                    Padding = new LiveChartsCore.Drawing.Padding(6)
                }
            };

                return result;
            }
        }
    }
