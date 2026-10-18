using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Maui;
using Syncfusion.Maui.Toolkit.Charts;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ToolTester.Domain.Entities;
using ToolTester.Presentation.Models;

namespace ToolTester.Presentation.PageModels
{
    public partial class ScanGridDetailPageModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ScanReport> scans = new();

        [ObservableProperty]
        private ScanReport? selectedScan;

        [ObservableProperty]
        private ISeries[] cweSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private Axis[] cweXAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] cweYAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private ISeries[] severitySeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string? errorMessage;

        // Basic stats
        [ObservableProperty]
        private int totalFindings;

        [ObservableProperty]
        private double meanFindingsPerCwe;

        [ObservableProperty]
        private double medianFindingsPerCwe;

        [ObservableProperty]
        private double stddevFindingsPerCwe;

        public ScanGridDetailPageModel()
        {
            ConfigureEmptyCharts();
            // For demo: load sample data
            _ = LoadSampleAsync();
        }

        private void ConfigureEmptyCharts()
        {
            CweSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Findings",
                    Values = Array.Empty<double>()
                }
            };

            CweXAxes = new Axis[]
            {
                new Axis { Labels = Array.Empty<string>(), LabelsRotation = 45 }
            };

            CweYAxes = new Axis[]
            {
                new Axis { MinLimit = 0 }
            };

            SeveritySeries = Array.Empty<ISeries>();
        }

        private async Task LoadSampleAsync()
        {
            try
            {
                IsLoading = true;
                await Task.Delay(100); // simulate IO

                // Replace this mock load with your mediator/query call
                var sample = new[]
                {
                    new ScanReport
                    {
                        Id = "scan-001",
                        Name = "Nightly Scan 2026-10-15",
                        FindingsCount = 120,
                        FindingsPerCwe = new Dictionary<string,int>
                        {
                            ["CWE-79"] = 30,
                            ["CWE-89"] = 25,
                            ["CWE-200"] = 20,
                            ["CWE-22"] = 15,
                            ["CWE-77"] = 10,
                            ["Other"] = 20
                        },
                        FindingsPerSeverity = new Dictionary<string,int>
                        {
                            ["High"] = 20,
                            ["Medium"] = 60,
                            ["Low"] = 40
                        }
                    },
                    new ScanReport
                    {
                        Id = "scan-002",
                        Name = "Ad-hoc Scan 2026-10-14",
                        FindingsCount = 45,
                        FindingsPerCwe = new Dictionary<string,int>
                        {
                            ["CWE-79"] = 10,
                            ["CWE-89"] = 5,
                            ["CWE-200"] = 8,
                            ["CWE-22"] = 7,
                            ["Other"] = 15
                        },
                        FindingsPerSeverity = new Dictionary<string,int>
                        {
                            ["High"] = 5,
                            ["Medium"] = 25,
                            ["Low"] = 15
                        }
                    }
                };

                Scans = new ObservableCollection<ScanReport>(sample);
                SelectedScan = Scans.FirstOrDefault();
                if (SelectedScan is not null) BuildChartsForSelected();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSelectedScanChanged(ScanReport? value)
        {
            BuildChartsForSelected();
        }

        private void BuildChartsForSelected()
        {
            if (SelectedScan is null)
            {
                ConfigureEmptyCharts();
                TotalFindings = 0;
                MeanFindingsPerCwe = MedianFindingsPerCwe = StddevFindingsPerCwe = 0;
                return;
            }

            // CWE distribution: top N by count
            var cwePairs = SelectedScan.FindingsPerCwe
                .OrderByDescending(kv => kv.Value)
                .ToArray();

            var labels = cwePairs.Select(kv => kv.Key).ToArray();
            var values = cwePairs.Select(kv => (double)kv.Value).ToArray();

            CweSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Findings",
                    Values = values
                },
                // cumulative line for Pareto-like view
                new LineSeries<double>
                {
                    Name = "Cumulative %",
                    Values = values
                        .Select((v, i) => values.Take(i + 1).Sum() / Math.Max(values.Sum(), 1) * 100.0)
                        .ToArray(),
                    ScalesYAt = 1,
                    GeometrySize = 5
                }
            };

            CweXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsRotation = 45,
                    Name = "CWE"
                }
            };

            CweYAxes = new Axis[]
            {
                new Axis { Name = "Count", MinLimit = 0 },
                new Axis { Name = "Cumulative %", MinLimit = 0, MaxLimit = 100 } // second axis for line
            };

            // Severity pie
            var severityPairs = SelectedScan.FindingsPerSeverity.ToArray();
            SeveritySeries = severityPairs.Select(kv =>
                (ISeries)new PieSeries<double>
                {
                    Name = kv.Key,
                    Values = new double[] { kv.Value },
                    DataLabelsPaint = null
                }).ToArray();

            // Basic stats
            TotalFindings = SelectedScan.FindingsCount;

            var perCwe = SelectedScan.FindingsPerCwe.Values.Select(v => (double)v).ToArray();
            MeanFindingsPerCwe = perCwe.Length == 0 ? 0 : perCwe.Average();
            MedianFindingsPerCwe = CalculateMedian(perCwe);
            StddevFindingsPerCwe = CalculateStdDev(perCwe);
        }

        private static double CalculateMedian(double[] values)
        {
            if (values.Length == 0) return 0;
            var sorted = values.OrderBy(v => v).ToArray();
            var mid = sorted.Length / 2;
            return (sorted.Length % 2) == 0
                ? (sorted[mid - 1] + sorted[mid]) / 2.0
                : sorted[mid];
        }

        private static double CalculateStdDev(double[] values)
        {
            if (values.Length == 0) return 0;
            var mean = values.Average();
            var variance = values.Sum(v => (v - mean) * (v - mean)) / values.Length;
            return Math.Sqrt(variance);
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            // Replace with your real refresh logic
            await LoadSampleAsync();
        }
    }
}