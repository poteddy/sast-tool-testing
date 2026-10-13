using ClosedXML.Excel;
using CommunityToolkit.Mvvm.Input;

using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Syncfusion.Maui.Toolkit.Charts;
using System.Collections.ObjectModel;
using ToolTester.Application.CWETestResultBases.Queries;
using ToolTester.Application.Reports.Quiries;
using ToolTester.Infrastructure.Persistance;
using ToolTester.Infrastructure.Services;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.Models.Charts;
using ToolTester.Presentation.Models.Reports;
using ToolTester.Presentation.Services;

namespace ToolTester.Presentation.PageModels;

public partial class ReportPageModel : BaseViewModel
{
    private const int TestResultPageSize = 250000;
    private const int ReportPageSize = 10000;
    private const int RelatedErrorValue = 0;
    private const int UnrelatedErrorValue = 5;
    private const int ParetoCweLimit = 25;
    private const string AllRelationships = "All";
     private const int DetectedWithoutCweErrorValue = 2;
    private static readonly string[] RelationshipDisplayOrder =
[
    nameof(CweRelationshipKind.Exact),

    nameof(CweRelationshipKind.DirectParent),
    nameof(CweRelationshipKind.DirectChild),
    nameof(CweRelationshipKind.Ancestor),
    nameof(CweRelationshipKind.Descendant),
    nameof(CweRelationshipKind.GrandParent),
    nameof(CweRelationshipKind.GrandChild),

    nameof(CweRelationshipKind.DirectSibling),
    nameof(CweRelationshipKind.SharedAncestor),
    nameof(CweRelationshipKind.PeerOf),

    nameof(CweRelationshipKind.CanPrecede),
    nameof(CweRelationshipKind.CanFollow),
    nameof(CweRelationshipKind.Requires),
    nameof(CweRelationshipKind.CanAlsoBe),

    nameof(CweRelationshipKind.SameRootCauseBroaderCwe),
    nameof(CweRelationshipKind.SameRootCauseNarrowerCwe),
    nameof(CweRelationshipKind.SameConsequence),
    nameof(CweRelationshipKind.SameWeaknessFamily),
    nameof(CweRelationshipKind.RelatedAttackPattern),

    nameof(CweRelationshipKind.RelatedWeakness),
    nameof(CweRelationshipKind.JulietRootCause),

    nameof(CweRelationshipKind.DetectedWithoutCwe),

    nameof(CweRelationshipKind.Unrelated)
];

    private static readonly SKColor[] ChartColors =
    [
        SKColors.SteelBlue,
        SKColors.ForestGreen,
        SKColors.DarkOrange,
        SKColors.MediumPurple,
        SKColors.Crimson,
        SKColors.Teal,
        SKColors.Goldenrod,
        SKColors.DeepPink
    ];

    private readonly BenchmarkReportService _benchmarkService;
    private readonly ModalErrorHandler _errorHandler;
    private readonly IMediator _mediator;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    private IReadOnlyDictionary<int, string> _scanNames =
        new Dictionary<int, string>();

    private ObservableCollection<CweTestResults> _items = [];
    private ObservableCollection<TestSeries> _testSeries = [];
    private ObservableCollection<AggrigatedSeries> _aggregatedSeries = [];
    private ObservableCollection<RelatedSeries> _relatedSeries = [];
    private ObservableCollection<FalseNegativeParetoSeries> _falseNegativeParetoSeries = [];
    private ObservableCollection<RelationshipMixSeries> _relationshipMixSeries = [];
    private ObservableCollection<RelationshipMixChartPoint> _relationshipMixChartData = [];
    private ObservableCollection<ParetoChartModel> _paretoCharts = [];
    private ObservableCollection<ChartLegendItem>  _relationshipMixLegendItems = [];

    private ObservableCollection<ChartLegendItem>  _falsePositiveLegendItems = [];
    private ISeries[] _falsePositiveSeries = [];
    private Axis[] _falsePositiveXAxes = [];
    private Axis[] _falsePositiveYAxes = [];
    private ISeries[] _relatedFindingSeries = [];
    private Axis[] _relatedFindingXAxes = [];
    private Axis[] _relatedFindingYAxes = [];
    private ISeries[] _relationshipMixChartSeries = [];
    private Axis[] _relationshipMixXAxes = [];
    private Axis[] _relationshipMixYAxes = [];
    private ISeries[] _polarSeries = [];
    private PolarAxis[] _polarAngleAxes = [];
    private PolarAxis[] _polarRadiusAxes = [];
    private ISeries[] _scannerCweHeatmapSeries = [];

    private Axis[] _scannerCweHeatmapXAxes = [];

    private Axis[] _scannerCweHeatmapYAxes = [];

    private ObservableCollection<CweScannerHeatmapPoint>
        _scannerCweHeatmapData = [];

    private ObservableCollection<ChartLegendItem>
    _relatedFindingLegendItems = [];

    private bool _hasScannerCweHeatmapData;



    private bool _isNavigatedTo;
    private bool _isLoading;
    private bool _dataLoaded;
    private string _selectedRelationship = AllRelationships;

    public ReportPageModel(
        ModalErrorHandler errorHandler,
        IMediator mediator,
        BenchmarkReportService benchmarkService,
        IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _errorHandler = errorHandler ??
            throw new ArgumentNullException(nameof(errorHandler));
        _mediator = mediator ??
            throw new ArgumentNullException(nameof(mediator));
        _benchmarkService = benchmarkService ??
            throw new ArgumentNullException(nameof(benchmarkService));
        _contextFactory = contextFactory ??
            throw new ArgumentNullException(nameof(contextFactory));
    }

    public event EventHandler? ReportDataLoaded;

    public bool HasLoadedData => _dataLoaded;

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public ObservableCollection<CweTestResults> Items
    {
        get => _items;
        private set => SetProperty(ref _items, value);
    }

    public ObservableCollection<TestSeries> TestSeries
    {
        get => _testSeries;
        private set => SetProperty(ref _testSeries, value);
    }

    public ObservableCollection<RelatedSeries> RelatedSeries
    {
        get => _relatedSeries;
        private set => SetProperty(ref _relatedSeries, value);
    }

    public ObservableCollection<AggrigatedSeries> AggregatedSeries
    {
        get => _aggregatedSeries;
        private set => SetProperty(ref _aggregatedSeries, value);
    }

    public ObservableCollection<FalseNegativeParetoSeries> FalseNegativeParetoSeries
    {
        get => _falseNegativeParetoSeries;
        private set => SetProperty(ref _falseNegativeParetoSeries, value);
    }

    public ObservableCollection<RelationshipMixSeries> RelationshipMixSeries
    {
        get => _relationshipMixSeries;
        private set => SetProperty(ref _relationshipMixSeries, value);
    }

    public ObservableCollection<RelationshipMixChartPoint> RelationshipMixChartData
    {
        get => _relationshipMixChartData;
        private set => SetProperty(ref _relationshipMixChartData, value);
    }

    public ObservableCollection<ParetoChartModel> ParetoCharts
    {
        get => _paretoCharts;
        private set => SetProperty(ref _paretoCharts, value);
    }

    public ObservableCollection<ChartLegendItem>
    RelationshipMixLegendItems
    {
        get => _relationshipMixLegendItems;

        private set => SetProperty(
            ref _relationshipMixLegendItems,
            value);
    }
    public ObservableCollection<ChartLegendItem>
    RelatedFindingLegendItems
    {
        get => _relatedFindingLegendItems;

        private set => SetProperty(
            ref _relatedFindingLegendItems,
            value);
    }
    public ObservableCollection<ChartLegendItem>
    FalsePositiveLegendItems
    {
        get => _falsePositiveLegendItems;

        private set => SetProperty(
            ref _falsePositiveLegendItems,
            value);
    }
    public ISeries[] FalsePositiveSeries
    {
        get => _falsePositiveSeries;
        private set => SetProperty(ref _falsePositiveSeries, value);
    }

    public Axis[] FalsePositiveXAxes
    {
        get => _falsePositiveXAxes;
        private set => SetProperty(ref _falsePositiveXAxes, value);
    }

    public Axis[] FalsePositiveYAxes
    {
        get => _falsePositiveYAxes;
        private set => SetProperty(ref _falsePositiveYAxes, value);
    }

    public ISeries[] RelatedFindingSeries
    {
        get => _relatedFindingSeries;
        private set => SetProperty(ref _relatedFindingSeries, value);
    }

    public Axis[] RelatedFindingXAxes
    {
        get => _relatedFindingXAxes;
        private set => SetProperty(ref _relatedFindingXAxes, value);
    }

    public Axis[] RelatedFindingYAxes
    {
        get => _relatedFindingYAxes;
        private set => SetProperty(ref _relatedFindingYAxes, value);
    }

    public ISeries[] RelationshipMixChartSeries
    {
        get => _relationshipMixChartSeries;
        private set => SetProperty(ref _relationshipMixChartSeries, value);
    }

    public Axis[] RelationshipMixXAxes
    {
        get => _relationshipMixXAxes;
        private set => SetProperty(ref _relationshipMixXAxes, value);
    }

    public Axis[] RelationshipMixYAxes
    {
        get => _relationshipMixYAxes;
        private set => SetProperty(ref _relationshipMixYAxes, value);
    }

    public ISeries[] PolarSeries
    {
        get => _polarSeries;
        private set => SetProperty(ref _polarSeries, value);
    }

    public PolarAxis[] PolarAngleAxes
    {
        get => _polarAngleAxes;
        private set => SetProperty(ref _polarAngleAxes, value);
    }

    public PolarAxis[] PolarRadiusAxes
    {
        get => _polarRadiusAxes;
        private set => SetProperty(ref _polarRadiusAxes, value);
    }
    public ISeries[] ScannerCweHeatmapSeries
    {
        get => _scannerCweHeatmapSeries;

        private set => SetProperty(
            ref _scannerCweHeatmapSeries,
            value);
    }

    public Axis[] ScannerCweHeatmapXAxes
    {
        get => _scannerCweHeatmapXAxes;

        private set => SetProperty(
            ref _scannerCweHeatmapXAxes,
            value);
    }

    public Axis[] ScannerCweHeatmapYAxes
    {
        get => _scannerCweHeatmapYAxes;

        private set => SetProperty(
            ref _scannerCweHeatmapYAxes,
            value);
    }

    public ObservableCollection<CweScannerHeatmapPoint>
        ScannerCweHeatmapData
    {
        get => _scannerCweHeatmapData;

        private set => SetProperty(
            ref _scannerCweHeatmapData,
            value);
    }

    public bool HasScannerCweHeatmapData
    {
        get => _hasScannerCweHeatmapData;

        private set => SetProperty(
            ref _hasScannerCweHeatmapData,
            value);
    }

    public string SelectedRelationship
    {
        get => _selectedRelationship;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? AllRelationships
                : value;

            if (!SetProperty(ref _selectedRelationship, normalized))
            {
                return;
            }

            if (_dataLoaded)
            {
                BuildRelatedFindingChart();
            }
        }
    }

    public List<string> AvailableRelationships =>
    [
        AllRelationships,
        .. RelatedSeries
            .SelectMany(series => series.Items)
            .Select(item => item.Relationship)
            .Where(relationship => !string.IsNullOrWhiteSpace(relationship))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(relationship => relationship, StringComparer.OrdinalIgnoreCase)
    ];

    public async Task InitializeAsync()
    {
        if (_dataLoaded || IsLoading)
        {
            return;
        }

        await ExecuteLoadAsync();
    }

    public async Task LoadItemsAsync(
       CancellationToken cancellationToken = default)
    {
        await LoadScanNamesAsync(cancellationToken);

        var testResult = await _mediator.Send(
            new GetCweTestResultBasesWithPaginationQuery
            {
                PageSize = TestResultPageSize
            },
            cancellationToken);

        var reportResult = await _mediator.Send(
            new GetReportsWithPaginationQuery
            {
                PageSize = ReportPageSize
            },
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var reportLookup = reportResult.Items
            .GroupBy(report => (
                report.ScanId,
                ScannerCweId: report.ScannerCweId,
                GroundTruthCweId: report.GroundTruthCweId))
            .ToDictionary(
                group => group.Key,
                group => group.First());

        RelatedSeries =
            new ObservableCollection<RelatedSeries>(
                reportResult.Items
                    .GroupBy(report => report.ScanId)
                    .OrderBy(group => group.Key)
                    .Select(group => new RelatedSeries
                    {
                        ScanId = group.Key,

                        ScanName =
                            GetScanName(group.Key),

                        Items = group
                            .OrderBy(report =>
                                report.GroundTruthCweId)
                            .ThenBy(report =>
                                report.ScannerCweId)
                            .Select(report =>
                                new RelatedItemsInTest
                                {
                                    Id =
                                        report.Id,

                                    GroundTruthCweId =
                                        report.GroundTruthCweId,

                                    ScannerCweId =
                                        report.ScannerCweId,

                                    ScanId =
                                        report.ScanId,

                                    ToolId =
                                        report.ToolId,

                                    Count =
                                        report.Count,

                                    Relationship =
                                        report.Relationship,

                                    RelationshipScore =
                                        report.RelationshipScore
                                })
                            .ToList()
                    }));

        OnPropertyChanged(
            nameof(AvailableRelationships));

        BuildRelationshipMix();

        var mappedItems = testResult.Items
        .Select(item =>
        {
            var reportKey = (
                ScanId: item.ScanId,
                ScannerCweId: item.ScannerFoundCWE,
                GroundTruthCweId: item.GroundTruthCwe);

            var hasSavedReport =
                reportLookup.TryGetValue(
                    reportKey,
                    out var savedReport);

            var detectedWithoutCwe =
                !item.ScannerFoundCWE.HasValue ||
                (
                    hasSavedReport &&
                    string.Equals(
                        savedReport!.Relationship,
                        nameof(CweRelationshipKind.DetectedWithoutCwe),
                        StringComparison.OrdinalIgnoreCase)
                );

            var errorValue =
                detectedWithoutCwe
                    ? DetectedWithoutCweErrorValue
                    : hasSavedReport &&
                      savedReport!.RelationshipScore > 0
                        ? RelatedErrorValue
                        : UnrelatedErrorValue;

            return new CweTestResults
            {
                Id =
                    item.Id,

                ScanId =
                    item.ScanId,

                TestId =
                    item.TestId,

                ScannerFoundCWE =
                    item.ScannerFoundCWE,

                GroundTruthCwe =
                    item.GroundTruthCwe,

                TestFileName =
                    item.TestFileName,

                TestPath =
                    item.TestPath,

                ErrorValue =
                    errorValue
            };
        })
        .ToList();

        Items =
            new ObservableCollection<CweTestResults>(
                mappedItems);

        TestSeries =
            new ObservableCollection<TestSeries>(
                mappedItems
                    .GroupBy(item => item.ScanId)
                    .OrderBy(group => group.Key)
                    .Select(group => new TestSeries
                    {
                        ScanId =
                            group.Key,

                        ScanName =
                            GetScanName(group.Key),

                        Items =
                            group.ToList()
                    }));

        AggregatedSeries =
     new ObservableCollection<AggrigatedSeries>(
         testResult.Items
             .GroupBy(item => item.ScanId)
             .OrderBy(group => group.Key)
             .Select(group => new AggrigatedSeries
             {
                 ScanId =
                     group.Key,

                 ScanName =
                     GetScanName(group.Key),

                 Items = group
                     .Where(item =>
                         item.ScannerFoundCWE.HasValue)
                     .GroupBy(item =>
                         item.ScannerFoundCWE!.Value)
                     .OrderBy(cweGroup =>
                         cweGroup.Key)
                     .Select(cweGroup =>
                         new AggrigatedItems
                         {
                             CweId =
                                 cweGroup.Key,

                             Count =
                                 cweGroup.Count()
                         })
                     .ToList()
             }));
    }

    private void BuildFalseNegativePareto(
        IEnumerable<ScannerFalseNegativeDto> data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var scannerSeries = data
            .Where(item => item.FalseNegatives > 0)
            .GroupBy(item => item.ScanId)
            .OrderBy(group => group.Key)
            .Select(scannerGroup =>
            {
                var scanId = scannerGroup.Key;
                var scanName = GetScanName(scanId);
                var scanDisplayName = GetScanDisplayName(scanId);

                var topMisses = scannerGroup
                    .GroupBy(item => new
                    {
                        item.CweId,
                        item.CweName
                    })
                    .Select(cweGroup => new
                    {
                        cweGroup.Key.CweId,
                        CweName = string.IsNullOrWhiteSpace(cweGroup.Key.CweName)
                            ? $"CWE-{cweGroup.Key.CweId}"
                            : cweGroup.Key.CweName,
                        Opportunities = cweGroup.Sum(item => item.Opportunities),
                        Detected = cweGroup.Sum(item => item.Detected),
                        FalseNegatives = cweGroup.Sum(item => item.FalseNegatives)
                    })
                    .Where(item => item.FalseNegatives > 0)
                    .OrderByDescending(item => item.FalseNegatives)
                    .ThenBy(item => item.CweId)
                    .Take(ParetoCweLimit)
                    .ToList();

                var totalFalseNegatives =
                    topMisses.Sum(item => item.FalseNegatives);
                var runningFalseNegatives = 0;

                var points = topMisses
                    .Select(item =>
                    {
                        runningFalseNegatives += item.FalseNegatives;

                        var cumulativePercent = totalFalseNegatives == 0
                            ? 0
                            : runningFalseNegatives * 100.0 /
                              totalFalseNegatives;

                        return new FalseNegativeChartPoint
                        {
                            ScanId = scanId,
                            ScannerName = scanDisplayName,
                            CweId = item.CweId,
                            CweName = item.CweName,
                            CweLabel = $"CWE-{item.CweId}",
                            Opportunities = item.Opportunities,
                            Detected = item.Detected,
                            FalseNegatives = item.FalseNegatives,
                            CumulativePercent = cumulativePercent
                        };
                    })
                    .ToList();

                return new FalseNegativeParetoSeries
                {
                    ScanId = scanId,
                    ScanName = scanName,
                    ScannerName = scanDisplayName,
                    Items = points
                };
            })
            .ToList();

        FalseNegativeParetoSeries =
            new ObservableCollection<FalseNegativeParetoSeries>(scannerSeries);
    }

    private void BuildRelationshipMix()
    {
        var chartPoints =
            new List<RelationshipMixChartPoint>();

        var mixSeries =
            new List<RelationshipMixSeries>();

        foreach (var relatedSeries in
                 RelatedSeries.OrderBy(
                     series => series.ScanId))
        {
            var scanId =
                relatedSeries.ScanId;

            var scanName =
                GetScanName(scanId);

            var scanDisplayName =
                GetScanDisplayName(scanId);

            var relationshipCounts =
                relatedSeries.Items
                    .Where(item =>
                        !string.IsNullOrWhiteSpace(
                            item.Relationship))
                    .GroupBy(
                        item => item.Relationship,
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Sum(
                            item => item.Count),
                        StringComparer.OrdinalIgnoreCase);

            var totalCount =
                relationshipCounts.Values.Sum();

            if (totalCount <= 0)
            {
                continue;
            }

            /*
             * Start with the preferred display order, then append any
             * relationship returned by reporting that is not already
             * represented.
             */
            var orderedRelationships =
                RelationshipDisplayOrder
                    .Concat(
                        relationshipCounts.Keys
                            .Where(relationship =>
                                !RelationshipDisplayOrder.Contains(
                                    relationship,
                                    StringComparer.OrdinalIgnoreCase))
                            .OrderBy(
                                relationship => relationship,
                                StringComparer.OrdinalIgnoreCase))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            var runningPercent = 0.0;

            var seriesPoints =
                new List<RelationshipMixPoint>();

            foreach (var relationship in
                     orderedRelationships)
            {
                relationshipCounts.TryGetValue(
                    relationship,
                    out var count);

                /*
                 * Do not add empty categories that were not actually
                 * reported for this scan.
                 */
                if (count <= 0)
                {
                    continue;
                }

                var percentage =
                    count * 100.0 / totalCount;

                var low =
                    runningPercent;

                var high =
                    runningPercent + percentage;

                seriesPoints.Add(
                    new RelationshipMixPoint
                    {
                        Relationship =
                            relationship,

                        Count =
                            count,

                        Percentage =
                            percentage
                    });

                chartPoints.Add(
                    new RelationshipMixChartPoint
                    {
                        ScanId =
                            scanId,

                        ScanName =
                            scanName,

                        ScannerName =
                            scanDisplayName,

                        Relationship =
                            relationship,

                        Count =
                            count,

                        Percentage =
                            percentage,

                        Low =
                            low,

                        High =
                            high
                    });

                runningPercent =
                    high;
            }

            mixSeries.Add(
                new RelationshipMixSeries
                {
                    ScanId =
                        scanId,

                    ScanName =
                        scanName,

                    Items =
                        seriesPoints
                });
        }

        RelationshipMixSeries =
            new ObservableCollection<RelationshipMixSeries>(
                mixSeries);

        RelationshipMixChartData =
            new ObservableCollection<RelationshipMixChartPoint>(
                chartPoints);
    }

    private void BuildLiveCharts()
    {
        BuildFalsePositiveChart();
        BuildRelatedFindingChart();
        BuildParetoCharts();
        BuildRelationshipMixLiveChart();
        BuildPolarLiveChart();
    }

    private void BuildFalsePositiveChart()
    {
        // Preserve scanner visibility if the chart gets rebuilt.
        var previousVisibility = FalsePositiveLegendItems
            .GroupBy(
                item => item.Name,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().IsVisible,
                StringComparer.OrdinalIgnoreCase);

        var chartSeries = new List<ISeries>();

        var legendItems =
            new ObservableCollection<ChartLegendItem>();

        var colorIndex = 0;

        foreach (var dataSeries in AggregatedSeries
                     .OrderBy(series => series.ScanId))
        {
            if (dataSeries.Items.Count == 0)
            {
                continue;
            }

            var color =
                GetChartColor(colorIndex++);

            var seriesName =
                GetScanDisplayName(dataSeries.ScanId);

            var isVisible =
                !previousVisibility.TryGetValue(
                    seriesName,
                    out var previousValue) ||
                previousValue;

            var series =
                new ScatterSeries<ObservablePoint>
                {
                    Name = seriesName,

                    Values = dataSeries.Items
                        .Select(item =>
                            new ObservablePoint(
                                item.CweId,
                                item.Count))
                        .ToArray(),

                    IsVisible = isVisible,

                    IsVisibleAtLegend = true,

                    GeometrySize = 12,

                    Fill =
                        new SolidColorPaint(color),

                    Stroke =
                        new SolidColorPaint(color)
                        {
                            StrokeThickness = 1
                        },

                    YToolTipLabelFormatter =
                        point =>
                            $"Scan: {seriesName}" +
                            $"{Environment.NewLine}" +
                            $"CWE: " +
                            $"{point.Coordinate.SecondaryValue:F0}" +
                            $"{Environment.NewLine}" +
                            $"Count: " +
                            $"{point.Coordinate.PrimaryValue:F0}"
                };

            chartSeries.Add(series);

            legendItems.Add(
                new ChartLegendItem(
                    series,
                    ToMauiColor(color),
                    isVisible));
        }

        FalsePositiveSeries =
            chartSeries.ToArray();

        FalsePositiveLegendItems =
            legendItems;

        FalsePositiveXAxes =
        [
            CreateNumericAxis(
            "Scanner CWE")
        ];

        FalsePositiveYAxes =
        [
            CreateNumericAxis(
            "Finding Count",
            0)
        ];
    }

    private void BuildRelatedFindingChart()
    {
        // Preserve scanner visibility when the relationship filter changes.
        var previousVisibility = RelatedFindingLegendItems
            .GroupBy(
                item => item.Name,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().IsVisible,
                StringComparer.OrdinalIgnoreCase);

        var chartSeries = new List<ISeries>();

        var legendItems =
            new ObservableCollection<ChartLegendItem>();

        var colorIndex = 0;

        foreach (var dataSeries in RelatedSeries
                     .OrderBy(series => series.ScanId))
        {
            var filteredItems = string.Equals(
                SelectedRelationship,
                AllRelationships,
                StringComparison.OrdinalIgnoreCase)
                    ? dataSeries.Items
                    : dataSeries.Items
                        .Where(item => string.Equals(
                            item.Relationship,
                            SelectedRelationship,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();

            /*
             * A scatter coordinate requires a scanner CWE.
             *
             * DetectedWithoutCwe rows have ScannerCweId = null,
             * so they cannot be plotted on the Scanner CWE axis.
             */
            var plottableItems = filteredItems
                .Where(item => item.ScannerCweId.HasValue)
                .ToList();

            if (plottableItems.Count == 0)
            {
                continue;
            }

            var lookup = plottableItems
                .GroupBy(item => CoordinateKey(
                    item.GroundTruthCweId,
                    item.ScannerCweId!.Value))
                .ToDictionary(
                    group => group.Key,
                    group => group.First());

            var color = GetChartColor(colorIndex++);

            var seriesName = GetScanDisplayName(
                dataSeries.ScanId);

            var isVisible =
                !previousVisibility.TryGetValue(
                    seriesName,
                    out var previousValue) ||
                previousValue;

            var series = new ScatterSeries<WeightedPoint>
            {
                Name = seriesName,

                Values = plottableItems
                    .Select(item => new WeightedPoint(
                        item.GroundTruthCweId,
                        item.ScannerCweId!.Value,
                        Math.Max(item.Count, 1)))
                    .ToArray(),

                IsVisible = isVisible,

                IsVisibleAtLegend = true,

                MinGeometrySize = 8,

                GeometrySize = 30,

                Fill = new SolidColorPaint(
                    color.WithAlpha(150)),

                Stroke = new SolidColorPaint(color)
                {
                    StrokeThickness = 1
                },

                YToolTipLabelFormatter = point =>
                {
                    var key = CoordinateKey(
                        point.Coordinate.SecondaryValue,
                        point.Coordinate.PrimaryValue);

                    if (!lookup.TryGetValue(key, out var item))
                    {
                        return $"Scan: {seriesName}";
                    }

                    return
                        $"Scan: {seriesName}" +
                        $"{Environment.NewLine}" +
                        $"Ground Truth CWE: {item.GroundTruthCweId}" +
                        $"{Environment.NewLine}" +
                        $"Scanner CWE: {item.ScannerCweId!.Value}" +
                        $"{Environment.NewLine}" +
                        $"Relationship: {item.Relationship}" +
                        $"{Environment.NewLine}" +
                        $"Confidence: {item.RelationshipScore}" +
                        $"{Environment.NewLine}" +
                        $"Count: {item.Count:N0}";
                }
            };

            chartSeries.Add(series);

            legendItems.Add(
                new ChartLegendItem(
                    series,
                    ToMauiColor(color),
                    isVisible));
        }

        RelatedFindingSeries =
            chartSeries.ToArray();

        RelatedFindingLegendItems =
            legendItems;

        RelatedFindingXAxes =
        [
            CreateNumericAxis(
            name: "Ground Truth CWE",
            labeler: value => $"CWE-{value:N0}")
        ];

        RelatedFindingYAxes =
        [
            CreateNumericAxis(
            name: "Scanner CWE",
            labeler: value => $"CWE-{value:N0}")
        ];
    }
    private void BuildParetoCharts()
    {
        var charts = new ObservableCollection<ParetoChartModel>();

        foreach (var scannerSeries in FalseNegativeParetoSeries)
        {
            var items = scannerSeries.Items
                .OrderByDescending(item => item.FalseNegatives)
                .ThenBy(item => item.CweId)
                .ToList();

            if (items.Count == 0)
            {
                continue;
            }

            var itemLookup = items
                .Select((item, itemIndex) => new { itemIndex, item })
                .ToDictionary(entry => entry.itemIndex, entry => entry.item);

            var falseNegativeValues = items
                .Select((item, itemIndex) =>
                    new ObservablePoint(itemIndex, item.FalseNegatives))
                .ToArray();

            var cumulativeValues = items
                .Select((item, itemIndex) =>
                    new ObservablePoint(itemIndex, item.CumulativePercent))
                .ToArray();

            var falseNegatives = new ColumnSeries<ObservablePoint>
            {
                Name = "False Negatives",
                Values = falseNegativeValues,
                ScalesYAt = 0,
                Fill = new SolidColorPaint(SKColors.SteelBlue),
                Stroke = null,
                MaxBarWidth = 45,
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                DataLabelsPosition = DataLabelsPosition.Top,
                DataLabelsFormatter = point =>
                    point.Coordinate.PrimaryValue.ToString("N0"),
                XToolTipLabelFormatter = point =>
                {
                    var itemIndex = Convert.ToInt32(Math.Round(
                        point.Coordinate.SecondaryValue));

                    if (!itemLookup.TryGetValue(itemIndex, out var item))
                    {
                        return $"False Negatives: " +
                               $"{point.Coordinate.PrimaryValue:F0}";
                    }

                    return
                        $"Scanner: {item.ScannerName}" +
                        $"{Environment.NewLine}Ground Truth CWE: {item.CweLabel}" +
                        $"{Environment.NewLine}CWE Name: {item.CweName}" +
                        $"{Environment.NewLine}Known Opportunities: {item.Opportunities}" +
                        $"{Environment.NewLine}Detected: {item.Detected}" +
                        $"{Environment.NewLine}False Negatives: {item.FalseNegatives}";
                }
            };

            var cumulative = new LineSeries<ObservablePoint>
            {
                Name = "Cumulative %",
                Values = cumulativeValues,
                ScalesYAt = 1,
                Fill = null,
                LineSmoothness = 0,
                Stroke = new SolidColorPaint(SKColors.DarkOrange)
                {
                    StrokeThickness = 3
                },
                GeometryFill = new SolidColorPaint(SKColors.DarkOrange),
                GeometryStroke = new SolidColorPaint(SKColors.White)
                {
                    StrokeThickness = 2
                },
                GeometrySize = 10,
                XToolTipLabelFormatter = point =>
                {
                    var itemIndex = Convert.ToInt32(Math.Round(
                        point.Coordinate.SecondaryValue));

                    if (!itemLookup.TryGetValue(itemIndex, out var item))
                    {
                        return $"Cumulative: " +
                               $"{point.Coordinate.PrimaryValue:F2}%";
                    }

                    return
                        $"Scanner: {item.ScannerName}" +
                        $"{Environment.NewLine}Ground Truth CWE: {item.CweLabel}" +
                        $"{Environment.NewLine}Cumulative: " +
                        $"{item.CumulativePercent:F2}%";
                }
            };

            charts.Add(new ParetoChartModel
            {
                Title = $"False Negatives Pareto - {scannerSeries.ScannerName}",
                Series = [falseNegatives, cumulative],
                XAxes =
                [
                    new Axis
                    {
                        Name = "Juliet Ground-Truth CWE",
                        Labels = items.Select(item => item.CweLabel).ToArray(),
                        LabelsRotation = -45,
                        MinStep = 1,
                        ForceStepToMin = true,
                        LabelsPaint = CreateTextPaint(),
                        NamePaint = CreateTextPaint(),
                        SeparatorsPaint = null
                    }
                ],
                YAxes =
                [
                    new Axis
                    {
                        Name = "False Negatives",
                        MinLimit = 0,
                        LabelsPaint = CreateTextPaint(),
                        NamePaint = CreateTextPaint(),
                        SeparatorsPaint = CreateSeparatorPaint(),
                        Labeler = value => value.ToString("N0")
                    },
                    new Axis
                    {
                        Name = "Cumulative Percentage",
                        Position = AxisPosition.End,
                        MinLimit = 0,
                        MaxLimit = 100,
                        MinStep = 20,
                        ForceStepToMin = true,
                        LabelsPaint = new SolidColorPaint(SKColors.DarkOrange),
                        NamePaint = new SolidColorPaint(SKColors.DarkOrange),
                        SeparatorsPaint = null,
                        Labeler = value => $"{value:F0}%"
                    }
                ]
            });
        }

        ParetoCharts = charts;
    }

    private void BuildRelationshipMixLiveChart()
    {
        var previousVisibility =
            RelationshipMixLegendItems
                .GroupBy(
                    item => item.Name,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().IsVisible,
                    StringComparer.OrdinalIgnoreCase);

        /*
         * IMPORTANT:
         * scannerNames defines the X-axis and also determines the order of
         * every value in every stacked series.
         */
        var scannerNames =
            RelationshipMixChartData
                .Select(item => item.ScanName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    name => name,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        RelationshipMixXAxes =
        [
            new Axis
        {
            Name = "Scanner",

            Labels = scannerNames,

            MinLimit = -0.5,

            MaxLimit = scannerNames.Length == 0
                ? 0.5
                : scannerNames.Length - 0.5,

            MinStep = 1,

            ForceStepToMin = true,

            LabelsRotation = -45,

            LabelsPaint = CreateTextPaint(),

            NamePaint = CreateTextPaint(),

            SeparatorsPaint = null
        }
        ];

        RelationshipMixYAxes =
        [
            new Axis
        {
            Name = "Relationship Mix",

            MinLimit = 0,

            MaxLimit = 100,

            MinStep = 10,

            ForceStepToMin = true,

            LabelsPaint = CreateTextPaint(),

            NamePaint = CreateTextPaint(),

            SeparatorsPaint = CreateSeparatorPaint(),

            Labeler = value => $"{value:F0}%"
        }
        ];

        var definitions = new[]
        {
        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.Exact),
            SKColors.SteelBlue),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.DirectParent),
            SKColors.Gold),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.DirectChild),
            SKColors.DarkOrange),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.Ancestor),
            SKColors.Goldenrod),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.Descendant),
            SKColors.OrangeRed),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.GrandParent),
            SKColors.Khaki),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.GrandChild),
            SKColors.Coral),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.DirectSibling),
            SKColors.ForestGreen),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.SharedAncestor),
            SKColors.Gray),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.PeerOf),
            SKColors.SeaGreen),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.CanPrecede),
            SKColors.Purple),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.CanFollow),
            SKColors.MediumPurple),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.Requires),
            SKColors.DarkViolet),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.CanAlsoBe),
            SKColors.Plum),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.SameRootCauseBroaderCwe),
            SKColors.LightBlue),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.SameRootCauseNarrowerCwe),
            SKColors.DeepSkyBlue),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.SameConsequence),
            SKColors.CadetBlue),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.SameWeaknessFamily),
            SKColors.LightSeaGreen),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.RelatedAttackPattern),
            SKColors.MediumTurquoise),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.RelatedWeakness),
            SKColors.SlateGray),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.JulietRootCause),
            SKColors.DarkCyan),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.DetectedWithoutCwe),
            SKColors.DimGray),

        new RelationshipChartDefinition(
            nameof(CweRelationshipKind.Unrelated),
            SKColors.Red)
    };

        var chartSeries =
            new List<ISeries>();

        var legendItems =
            new ObservableCollection<ChartLegendItem>();

        foreach (var definition in definitions)
        {
            /*
             * Build one value for every scanner.
             *
             * The index in this array corresponds exactly to the index in
             * scannerNames and therefore to the X-axis position.
             */
            var values =
                scannerNames
                    .Select(scannerName =>
                        RelationshipMixChartData
                            .FirstOrDefault(point =>
                                string.Equals(
                                    point.ScanName,
                                    scannerName,
                                    StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(
                                    point.Relationship,
                                    definition.Relationship,
                                    StringComparison.OrdinalIgnoreCase))
                            ?.Percentage ?? 0)
                    .ToArray();

            /*
             * Don't create a series if this relationship does not exist
             * for any scanner.
             */
            if (values.All(value => value <= 0))
            {
                continue;
            }

            var isVisible =
                !previousVisibility.TryGetValue(
                    definition.Relationship,
                    out var previousValue) ||
                previousValue;

            var series =
                new StackedColumnSeries<double>
                {
                    Name =
                        definition.Relationship,

                    Values =
                        values,

                    IsVisible =
                        isVisible,

                    /*
                     * Keep disabled series in our custom legend.
                     */
                    IsVisibleAtLegend =
                        true,

                    Fill =
                        new SolidColorPaint(
                            definition.Color),

                    Stroke =
                        null,

                    MaxBarWidth =
                        65,

                    /*
                     * Tooltip
                     *
                     * SecondaryValue is the X-axis position. Because
                     * scannerNames is used to build Values, this index maps
                     * directly back to the corresponding ScanName.
                     */
                    YToolTipLabelFormatter =
                        point =>
                        {
                            var scannerIndex =
                                Convert.ToInt32(
                                    Math.Round(
                                        point.Coordinate.SecondaryValue));

                            if (scannerIndex < 0 ||
                                scannerIndex >= scannerNames.Length)
                            {
                                return
                                    $"Relationship: {definition.Relationship}" +
                                    $"{Environment.NewLine}" +
                                    $"Percentage: {point.Coordinate.PrimaryValue:F2}%";
                            }

                            var scanName =
                                scannerNames[scannerIndex];

                            /*
                             * IMPORTANT:
                             *
                             * Match using ScanName because scannerNames and
                             * the X-axis were constructed from ScanName.
                             *
                             * ScannerName is the display name stored on the
                             * underlying data point and can still be shown
                             * in the tooltip.
                             */
                            var sourcePoint =
                                RelationshipMixChartData
                                    .FirstOrDefault(item =>
                                        string.Equals(
                                            item.ScanName,
                                            scanName,
                                            StringComparison.OrdinalIgnoreCase) &&
                                        string.Equals(
                                            item.Relationship,
                                            definition.Relationship,
                                            StringComparison.OrdinalIgnoreCase));

                            if (sourcePoint is null)
                            {
                                return
                                    $"Scanner: {scanName}" +
                                    $"{Environment.NewLine}" +
                                    $"Relationship: {definition.Relationship}" +
                                    $"{Environment.NewLine}" +
                                    $"Percentage: {point.Coordinate.PrimaryValue:F2}%";
                            }

                            /*
                             * Prefer the friendly scanner display name when
                             * one exists. Fall back to ScanName otherwise.
                             */
                            var displayName =
                                !string.IsNullOrWhiteSpace(
                                    sourcePoint.ScannerName)
                                    ? sourcePoint.ScannerName
                                    : sourcePoint.ScanName;

                            return
                                $"Scanner: {displayName}" +
                                $"{Environment.NewLine}" +
                                $"Relationship: {sourcePoint.Relationship}" +
                                $"{Environment.NewLine}" +
                                $"Count: {sourcePoint.Count:N0}" +
                                $"{Environment.NewLine}" +
                                $"Percentage: {sourcePoint.Percentage:F2}%";
                        }
                };

            chartSeries.Add(series);

            legendItems.Add(
                new ChartLegendItem(
                    series,
                    ToMauiColor(definition.Color),
                    isVisible));
        }

        RelationshipMixChartSeries =
            chartSeries.ToArray();

        RelationshipMixLegendItems =
            legendItems;
    }

    private void BuildPolarLiveChart()
    {
        var chartSeries = new List<ISeries>();
        var index = 0;

        foreach (var dataSeries in AggregatedSeries)
        {
            if (dataSeries.Items.Count == 0)
            {
                continue;
            }

            var color = GetChartColor(index++);

            chartSeries.Add(new PolarLineSeries<ObservablePoint>
            {
                Name = $"Scan {dataSeries.ScanName}",
                Values = dataSeries.Items
                    .OrderBy(item => item.CweId)
                    .Select(item => new ObservablePoint(item.CweId, item.Count))
                    .ToArray(),
                IsClosed = true,
                LineSmoothness = 0,
                Fill = new SolidColorPaint(color.WithAlpha(50)),
                Stroke = new SolidColorPaint(color)
                {
                    StrokeThickness = 2
                },
                GeometryFill = new SolidColorPaint(color),
                GeometryStroke = new SolidColorPaint(SKColors.White)
                {
                    StrokeThickness = 1
                },
                GeometrySize = 8,

            });
        }

        PolarSeries = chartSeries.ToArray();

        PolarAngleAxes =
        [
            new PolarAxis
            {
                Name = "Scanner CWE",
                LabelsPaint = CreateTextPaint(),
                NamePaint = CreateTextPaint(),
                SeparatorsPaint = CreateSeparatorPaint()
            }
        ];

        PolarRadiusAxes =
        [
            new PolarAxis
            {
                Name = "Finding Count",
                MinLimit = 0,
                LabelsPaint = CreateTextPaint(),
                NamePaint = CreateTextPaint(),
                SeparatorsPaint = CreateSeparatorPaint()
            }
        ];
    }
    private void BuildScannerCweHeatmap(
    IEnumerable<ScannerFalseNegativeDto> data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var source = data
            .Where(item => item.Opportunities > 0)
            .ToList();

        if (source.Count == 0)
        {
            ScannerCweHeatmapData = [];
            ScannerCweHeatmapSeries = [];
            ScannerCweHeatmapXAxes = [];
            ScannerCweHeatmapYAxes = [];
            HasScannerCweHeatmapData = false;

            return;
        }

        /*
         * A scan is treated as one scanner result set.
         *
         * Sorting by display name keeps the axis stable between
         * reloads when scan IDs are not sequential.
         */
        var scans = source
            .Select(item => item.ScanId)
            .Distinct()
            .Select(scanId => new
            {
                ScanId = scanId,
                ScanName = GetScanDisplayName(scanId)
            })
            .OrderBy(
                item => item.ScanName,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ScanId)
            .ToList();

        /*
         * Order CWEs numerically. This makes it easier to compare
         * the heatmap with the ground-truth CWE values shown by
         * the other charts.
         */
        var cwes = source
            .GroupBy(item => item.CweId)
            .Select(group => new
            {
                CweId = group.Key,

                CweName = group
                    .Select(item => item.CweName)
                    .FirstOrDefault(name =>
                        !string.IsNullOrWhiteSpace(name))
                    ?? $"CWE-{group.Key}"
            })
            .OrderBy(item => item.CweId)
            .ToList();

        var scanIndexById = scans
            .Select((scan, index) => new
            {
                scan.ScanId,
                Index = index
            })
            .ToDictionary(
                item => item.ScanId,
                item => item.Index);

        var cweIndexById = cwes
            .Select((cwe, index) => new
            {
                cwe.CweId,
                Index = index
            })
            .ToDictionary(
                item => item.CweId,
                item => item.Index);

        /*
         * Aggregate first because BenchmarkReportService may
         * return more than one row for a ScanId/CWE combination.
         */
        var cells = source
            .GroupBy(item => new
            {
                item.ScanId,
                item.CweId
            })
            .Select(group =>
            {
                var opportunities = group.Sum(
                    item => item.Opportunities);

                var detected = group.Sum(
                    item => item.Detected);

                var falseNegatives = group.Sum(
                    item => item.FalseNegatives);

                var detectionRate = opportunities <= 0
                    ? 0
                    : detected * 100.0 / opportunities;

                var cweName = group
                    .Select(item => item.CweName)
                    .FirstOrDefault(name =>
                        !string.IsNullOrWhiteSpace(name))
                    ?? $"CWE-{group.Key.CweId}";

                return new CweScannerHeatmapPoint
                {
                    ScanId =
                        group.Key.ScanId,

                    ScanName =
                        GetScanDisplayName(
                            group.Key.ScanId),

                    ScannerIndex =
                        scanIndexById[
                            group.Key.ScanId],

                    CweIndex =
                        cweIndexById[
                            group.Key.CweId],

                    CweId =
                        group.Key.CweId,

                    CweName =
                        cweName,

                    Opportunities =
                        opportunities,

                    Detected =
                        detected,

                    FalseNegatives =
                        falseNegatives,

                    DetectionRate =
                        Math.Clamp(
                            detectionRate,
                            0,
                            100)
                };
            })
            .OrderBy(item => item.CweIndex)
            .ThenBy(item => item.ScannerIndex)
            .ToList();

        ScannerCweHeatmapData =
            new ObservableCollection<CweScannerHeatmapPoint>(
                cells);

        var cellLookup = cells.ToDictionary(
            cell => CoordinateKey(
                cell.ScannerIndex,
                cell.CweIndex));

        var heatmapValues = cells
            .Select(cell => new WeightedPoint(
                cell.ScannerIndex,
                cell.CweIndex,
                cell.DetectionRate))
            .ToArray();

        ScannerCweHeatmapSeries =
        [
            new HeatSeries<WeightedPoint>
        {
            Name = "Juliet detection rate",

            Values = heatmapValues,

            /*
             * Low coverage is red, medium coverage is yellow,
             * and high coverage is green.
             */
           HeatMap =
[
    new LvcColor(198, 40, 40),
    new LvcColor(239, 108, 0),
    new LvcColor(253, 216, 53),
    new LvcColor(124, 179, 66),
    new LvcColor(27, 94, 32)
],


        

            YToolTipLabelFormatter = point =>
            {
                var scannerIndex = Convert.ToInt32(
                    Math.Round(
                        point.Coordinate.SecondaryValue));

                var cweIndex = Convert.ToInt32(
                    Math.Round(
                        point.Coordinate.PrimaryValue));

                var key = CoordinateKey(
                    scannerIndex,
                    cweIndex);

                if (!cellLookup.TryGetValue(
                        key,
                        out var item))
                {
                    return "No benchmark data";
                }

                return
                    $"Scanner: {item.ScanName}" +
                    $"{Environment.NewLine}" +
                    $"Ground truth: CWE-{item.CweId}" +
                    $"{Environment.NewLine}" +
                    $"CWE name: {item.CweName}" +
                    $"{Environment.NewLine}" +
                    $"Opportunities: {item.Opportunities:N0}" +
                    $"{Environment.NewLine}" +
                    $"Detected: {item.Detected:N0}" +
                    $"{Environment.NewLine}" +
                    $"False negatives: " +
                    $"{item.FalseNegatives:N0}" +
                    $"{Environment.NewLine}" +
                    $"Detection rate: " +
                    $"{item.DetectionRate:F2}%";
            }
        }
        ];

        ScannerCweHeatmapXAxes =
        [
            new Axis
        {
            Name = "Scanner / Scan",

            Labels = scans
                .Select(item => item.ScanName)
                .ToArray(),

            MinLimit = -0.5,

            MaxLimit = scans.Count - 0.5,

            MinStep = 1,

            ForceStepToMin = true,

            LabelsRotation = -45,

            LabelsPaint =
                CreateTextPaint(),

            NamePaint =
                CreateTextPaint(),

            SeparatorsPaint =
                new SolidColorPaint(
                    new SKColor(
                        225,
                        225,
                        225))
                {
                    StrokeThickness = 1
                },

            Padding =
                new LiveChartsCore.Drawing.Padding(8)
        }
        ];

        ScannerCweHeatmapYAxes =
        [
            new Axis
        {
            Name = "Juliet Ground-Truth CWE",

            Labels = cwes
                .Select(item =>
                    $"CWE-{item.CweId}")
                .ToArray(),

            MinLimit = -0.5,

            MaxLimit = cwes.Count - 0.5,

            MinStep = 1,

            ForceStepToMin = true,

            LabelsPaint =
                CreateTextPaint(),

            NamePaint =
                CreateTextPaint(),

            SeparatorsPaint =
                new SolidColorPaint(
                    new SKColor(
                        225,
                        225,
                        225))
                {
                    StrokeThickness = 1
                },

            Padding =
                new LiveChartsCore.Drawing.Padding(8)
        }
        ];

        HasScannerCweHeatmapData =
            cells.Count > 0;
    }

    private static readonly CweTop25Definition[] MitreCweTop25 =
[
new(1, 79, "Cross-site Scripting"),
    new(2, 89, "SQL Injection"),
    new(3, 352, "Cross-Site Request Forgery"),
    new(4, 862, "Missing Authorization"),
    new(5, 787, "Out-of-bounds Write"),
    new(6, 22, "Path Traversal"),
    new(7, 416, "Use After Free"),
    new(8, 125, "Out-of-bounds Read"),
    new(9, 78, "OS Command Injection"),
    new(10, 94, "Code Injection"),
    new(11, 120, "Classic Buffer Overflow"),
    new(12, 434, "Dangerous File Upload"),
    new(13, 476, "NULL Pointer Dereference"),
    new(14, 121, "Stack-based Buffer Overflow"),
    new(15, 502, "Deserialization of Untrusted Data"),
    new(16, 122, "Heap-based Buffer Overflow"),
    new(17, 863, "Incorrect Authorization"),
    new(18, 20, "Improper Input Validation"),
    new(19, 284, "Improper Access Control"),
    new(20, 200, "Exposure of Sensitive Information"),
    new(21, 306, "Missing Authentication"),
    new(22, 918, "Server-Side Request Forgery"),
    new(23, 77, "Command Injection"),
    new(24, 639, "Authorization Bypass"),
    new(25, 770, "Uncontrolled Resource Allocation")
];

    private ISeries[] _mitreTop25Series = [];
    private Axis[] _mitreTop25XAxes = [];
    private Axis[] _mitreTop25YAxes = [];
    private bool _hasMitreTop25Data;
    public ISeries[] MitreTop25Series
    {
        get =>
            _mitreTop25Series;

        private set =>
            SetProperty(
                ref _mitreTop25Series,
                value);
    }

    public Axis[] MitreTop25XAxes
    {
        get =>
            _mitreTop25XAxes;

        private set =>
            SetProperty(
                ref _mitreTop25XAxes,
                value);
    }

    public Axis[] MitreTop25YAxes
    {
        get =>
            _mitreTop25YAxes;

        private set =>
            SetProperty(
                ref _mitreTop25YAxes,
                value);
    }

    public bool HasMitreTop25Data
    {
        get =>
            _hasMitreTop25Data;

        private set =>
            SetProperty(
                ref _hasMitreTop25Data,
                value);
    }
    private void BuildMitreTop25Chart(
        IEnumerable<ScannerFalseNegativeDto> data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var source =
            data.ToList();

        var top25Ids =
            MitreCweTop25
                .Select(item =>
                    item.CweId)
                .ToHashSet();

        /*
         * Include only scanners having at least one Juliet C/C++
         * benchmark opportunity for a CWE in the MITRE Top 25.
         */
        var scannerGroups =
            source
                .Where(item =>
                    top25Ids.Contains(
                        item.CweId))
                .GroupBy(item =>
                    item.ScanId)
                .OrderBy(group =>
                    GetScanName(group.Key),
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(group =>
                    group.Key)
                .ToList();

        var chartSeries =
            new List<ISeries>();

        var colorIndex = 0;

        foreach (var scannerGroup in scannerGroups)
        {
            var scanId =
                scannerGroup.Key;

            var scanName =
                GetScanName(scanId);

            /*
             * Aggregate in case the benchmark service returns more
             * than one row for the same ScanId and CWE.
             */
            var cweResults =
                scannerGroup
                    .GroupBy(item =>
                        item.CweId)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group => new
                        {
                            Opportunities =
                                group.Sum(item =>
                                    item.Opportunities),

                            Detected =
                                group.Sum(item =>
                                    item.Detected),

                            FalseNegatives =
                                group.Sum(item =>
                                    item.FalseNegatives)
                        });

            var values =
                new List<ObservablePoint>();

            var tooltipData =
                new Dictionary<int, MitreTop25ChartPoint>();

            for (var index = 0;
                 index < MitreCweTop25.Length;
                 index++)
            {
                var definition =
                    MitreCweTop25[index];

                cweResults.TryGetValue(
                    definition.CweId,
                    out var result);

                var opportunities =
                    result?.Opportunities ?? 0;

                var detected =
                    result?.Detected ?? 0;

                var falseNegatives =
                    result?.FalseNegatives ?? 0;

                /*
                 * Use double.NaN when Juliet has no opportunity for
                 * this CWE. The line will not imply a zero-percent
                 * scanner result where no benchmark case exists.
                 */
                var detectionRate =
                    opportunities <= 0
                        ? double.NaN
                        : detected * 100.0 /
                          opportunities;

                values.Add(
                    new ObservablePoint(
                        index,
                        detectionRate));

                tooltipData[index] =
                    new MitreTop25ChartPoint
                    {
                        Rank =
                            definition.Rank,

                        CweId =
                            definition.CweId,

                        CweName =
                            definition.Name,

                        ScanId =
                            scanId,

                        ScanName =
                            scanName,

                        Opportunities =
                            opportunities,

                        Detected =
                            detected,

                        FalseNegatives =
                            falseNegatives,

                        DetectionRate =
                            opportunities <= 0
                                ? null
                                : detectionRate
                    };
            }

            var color =
                GetChartColor(
                    colorIndex++);

            chartSeries.Add(
                new LineSeries<ObservablePoint>
                {
                    Name =
                        scanName,

                    Values =
                        values,

                    LineSmoothness = 0,

                    Fill = null,

                    Stroke =
                        new SolidColorPaint(
                            color)
                        {
                            StrokeThickness = 3
                        },

                    GeometryFill =
                        new SolidColorPaint(
                            color),

                    GeometryStroke =
                        new SolidColorPaint(
                            SKColors.White)
                        {
                            StrokeThickness = 2
                        },

                    GeometrySize = 10,

                    /*
                     * This matches the formatter API already compiling
                     * for your LineSeries implementation.
                     */
                    YToolTipLabelFormatter =
                        point =>
                        {
                            var pointIndex =
                                Convert.ToInt32(
                                    Math.Round(
                                        point.Coordinate
                                            .SecondaryValue));

                            if (!tooltipData.TryGetValue(
                                    pointIndex,
                                    out var item))
                            {
                                return
                                    $"Scanner: {scanName}";
                            }

                            var coverage =
                                item.DetectionRate.HasValue
                                    ? $"{item.DetectionRate.Value:F2}%"
                                    : "No C/C++ benchmark opportunities";

                            return
                                $"Scanner: {item.ScanName}" +
                                $"{Environment.NewLine}" +
                                $"MITRE rank: {item.Rank}" +
                                $"{Environment.NewLine}" +
                                $"CWE: CWE-{item.CweId}" +
                                $"{Environment.NewLine}" +
                                $"Name: {item.CweName}" +
                                $"{Environment.NewLine}" +
                                $"Opportunities: {item.Opportunities}" +
                                $"{Environment.NewLine}" +
                                $"Detected: {item.Detected}" +
                                $"{Environment.NewLine}" +
                                $"False negatives: {item.FalseNegatives}" +
                                $"{Environment.NewLine}" +
                                $"Detection rate: {coverage}";
                        }
                });
        }

        MitreTop25Series =
            chartSeries.ToArray();

        var labels =
            MitreCweTop25
                .Select(item =>
                    $"#{item.Rank} CWE-{item.CweId}")
                .ToArray();

        MitreTop25XAxes =
        [
            new Axis
        {
            Name =
                "2025 MITRE CWE Top 25 rank",

            Labels =
                labels,

            MinLimit = -0.5,

            MaxLimit =
                labels.Length - 0.5,

            MinStep = 1,

            ForceStepToMin = true,

            LabelsRotation = -45,

            LabelsPaint =
                CreateTextPaint(),

            NamePaint =
                CreateTextPaint(),

            SeparatorsPaint = null
        }
        ];

        MitreTop25YAxes =
        [
            new Axis
        {
            Name =
                "Juliet C/C++ detection rate",

            MinLimit = 0,

            MaxLimit = 100,

            MinStep = 10,

            ForceStepToMin = true,

            LabelsPaint =
                CreateTextPaint(),

            NamePaint =
                CreateTextPaint(),

            SeparatorsPaint =
                CreateSeparatorPaint(),

            Labeler =
                value =>
                    $"{value:F0}%"
        }
        ];

        HasMitreTop25Data =
            chartSeries.Count > 0;
    }

    
    
    public async Task ForceReloadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        _dataLoaded = false;

        Items.Clear();
        TestSeries.Clear();
        RelatedSeries.Clear();
        AggregatedSeries.Clear();
        FalsePositiveLegendItems.Clear();
        FalseNegativeParetoSeries.Clear();
        RelationshipMixSeries.Clear();
        RelationshipMixChartData.Clear();
        RelationshipMixLegendItems.Clear();
        ParetoCharts.Clear();

        FalsePositiveSeries = [];
        RelatedFindingSeries = [];
        RelationshipMixChartSeries = [];
        PolarSeries = [];

        MitreTop25Series = [];
        MitreTop25XAxes = [];
        MitreTop25YAxes = [];
        HasMitreTop25Data = false;

        ScannerCweHeatmapData.Clear();

        ScannerCweHeatmapSeries = [];

        ScannerCweHeatmapXAxes = [];

        ScannerCweHeatmapYAxes = [];

        HasScannerCweHeatmapData = false;

        await ExecuteLoadAsync();
    }
    [RelayCommand]
    private async Task Refresh()
    {
        await ForceReloadAsync();
    }
    [RelayCommand]
    private void NavigatedTo()
    {
        _isNavigatedTo = true;
    }

    [RelayCommand]
    private void NavigatedFrom()
    {
        _isNavigatedTo = false;
    }

    [RelayCommand]
    private async Task LoadReports()
    {
        if (IsLoading)
        {
            return;
        }

        await ExecuteLoadAsync();
    }
    [RelayCommand]
    private void ShowAllFalsePositiveSeries()
    {
        SetLegendVisibility(
            FalsePositiveLegendItems,
            true);
    }

    [RelayCommand]
    private void HideAllFalsePositiveSeries()
    {
        SetLegendVisibility(
            FalsePositiveLegendItems,
            false);
    }
    private async Task ExecuteLoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;

        try
        {
            await Task.Yield();
            await LoadItemsAsync();

            var falseNegatives = await _benchmarkService
                .GetFalseNegativesByScannerAsync();

            BuildFalseNegativePareto(falseNegatives);

            BuildMitreTop25Chart(falseNegatives);
            
            BuildScannerCweHeatmap(falseNegatives);

            BuildLiveCharts();


            _dataLoaded = true;
            OnPropertyChanged(nameof(HasLoadedData));
            ReportDataLoaded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            _dataLoaded = false;
            OnPropertyChanged(nameof(HasLoadedData));
            _errorHandler.HandleError(exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;

        try
        {
            using var workbook = new XLWorkbook();

            AddResultsWorksheet(workbook);
            AddRelationshipsWorksheet(workbook);
            AddAggregatedWorksheet(workbook);
            AddFalseNegativesWorksheet(workbook);
            AddSummaryWorksheet(workbook);

            var filePath = Path.Combine(
                FileSystem.CacheDirectory,
                $"CWE_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            workbook.SaveAs(filePath);

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "CWE Benchmark Report",
                    File = new ShareFile(filePath)
                });
        }
        catch (Exception exception)
        {
            _errorHandler.HandleError(exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ShowAllRelationshipMixSeries()
    {
        SetLegendVisibility(
            RelationshipMixLegendItems,
            true);
    }

    [RelayCommand]
    private void HideAllRelationshipMixSeries()
    {
        SetLegendVisibility(
            RelationshipMixLegendItems,
            false);
    }
    [RelayCommand]
    private void ShowAllRelatedFindingSeries()
    {
        SetLegendVisibility(
            RelatedFindingLegendItems,
            true);
    }

    [RelayCommand]
    private void HideAllRelatedFindingSeries()
    {
        SetLegendVisibility(
            RelatedFindingLegendItems,
            false);
    }
    private static void SetLegendVisibility(
        IEnumerable<ChartLegendItem> legendItems,
        bool isVisible)
    {
        foreach (var legendItem in legendItems)
        {
            legendItem.SetVisibility(isVisible);
        }
    }
    private void AddResultsWorksheet(XLWorkbook workbook)
    {
        var worksheet = workbook.Worksheets.Add("Results");

        worksheet.Cell(1, 1).Value = "Ground Truth CWE";
        worksheet.Cell(1, 2).Value = "Scanner CWE";
        worksheet.Cell(1, 3).Value = "Scan Name";
        worksheet.Cell(1, 4).Value = "Scan Id";
        worksheet.Cell(1, 5).Value = "Error Value";
        ApplyHeaderStyle(worksheet.Range(1, 1, 1, 5));

        var row = 2;

        foreach (var item in Items)
        {
            worksheet.Cell(row, 1).Value = item.GroundTruthCwe;
            worksheet.Cell(row, 2).Value = item.ScannerFoundCWE;
            worksheet.Cell(row, 3).Value = GetScanName(item.ScanId);
            worksheet.Cell(row, 4).Value = item.ScanId;
            worksheet.Cell(row, 5).Value = item.ErrorValue;
            row++;
        }

        CreateTableIfDataExists(worksheet, "ResultsTable", row - 1, 5);
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
    }

    private void AddRelationshipsWorksheet(XLWorkbook workbook)
    {
        var worksheet = workbook.Worksheets.Add("Relationships");

        worksheet.Cell(1, 1).Value = "Scan Name";
        worksheet.Cell(1, 2).Value = "Scan Id";
        worksheet.Cell(1, 3).Value = "Ground Truth CWE";
        worksheet.Cell(1, 4).Value = "Scanner CWE";
        worksheet.Cell(1, 5).Value = "Relationship";
        worksheet.Cell(1, 6).Value = "Score";
        worksheet.Cell(1, 7).Value = "Count";
        ApplyHeaderStyle(worksheet.Range(1, 1, 1, 7));

        var row = 2;

        foreach (var series in RelatedSeries)
        {
            foreach (var item in series.Items)
            {
                worksheet.Cell(row, 1).Value = series.ScanName;
                worksheet.Cell(row, 2).Value = series.ScanId;
                worksheet.Cell(row, 3).Value = item.GroundTruthCweId;
                worksheet.Cell(row, 4).Value = item.ScannerCweId;
                worksheet.Cell(row, 5).Value = item.Relationship;
                worksheet.Cell(row, 6).Value = item.RelationshipScore;
                worksheet.Cell(row, 7).Value = item.Count;
                row++;
            }
        }

        CreateTableIfDataExists(worksheet, "RelationshipsTable", row - 1, 7);
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
    }

    private void AddAggregatedWorksheet(XLWorkbook workbook)
    {
        var worksheet = workbook.Worksheets.Add("Aggregated");

        worksheet.Cell(1, 1).Value = "Scan Name";
        worksheet.Cell(1, 2).Value = "Scan Id";
        worksheet.Cell(1, 3).Value = "CWE";
        worksheet.Cell(1, 4).Value = "Count";
        ApplyHeaderStyle(worksheet.Range(1, 1, 1, 4));

        var row = 2;

        foreach (var series in AggregatedSeries)
        {
            foreach (var item in series.Items)
            {
                worksheet.Cell(row, 1).Value = series.ScanName;
                worksheet.Cell(row, 2).Value = series.ScanId;
                worksheet.Cell(row, 3).Value = item.CweId;
                worksheet.Cell(row, 4).Value = item.Count;
                row++;
            }
        }

        CreateTableIfDataExists(worksheet, "AggregatedTable", row - 1, 4);
        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
    }

    private void AddFalseNegativesWorksheet(XLWorkbook workbook)
    {
        var worksheet = workbook.Worksheets.Add("False Negatives");

        worksheet.Cell(1, 1).Value = "Scan Name";
        worksheet.Cell(1, 2).Value = "Scan Id";
        worksheet.Cell(1, 3).Value = "CWE";
        worksheet.Cell(1, 4).Value = "CWE Name";
        worksheet.Cell(1, 5).Value = "Known Opportunities";
        worksheet.Cell(1, 6).Value = "Detected";
        worksheet.Cell(1, 7).Value = "False Negatives";
        worksheet.Cell(1, 8).Value = "Cumulative Percent";
        ApplyHeaderStyle(worksheet.Range(1, 1, 1, 8));

        var row = 2;

        foreach (var series in FalseNegativeParetoSeries)
        {
            foreach (var item in series.Items)
            {
                worksheet.Cell(row, 1).Value = series.ScanName;
                worksheet.Cell(row, 2).Value = series.ScanId;
                worksheet.Cell(row, 3).Value = item.CweLabel;
                worksheet.Cell(row, 4).Value = item.CweName;
                worksheet.Cell(row, 5).Value = item.Opportunities;
                worksheet.Cell(row, 6).Value = item.Detected;
                worksheet.Cell(row, 7).Value = item.FalseNegatives;
                worksheet.Cell(row, 8).Value = item.CumulativePercent / 100.0;
                row++;
            }
        }

        if (row > 2)
        {
            worksheet.Range(2, 8, row - 1, 8)
                .Style.NumberFormat.Format = "0.00%";
        }

        CreateTableIfDataExists(
            worksheet,
            "FalseNegativesTable",
            row - 1,
            8);

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
    }

    private void AddSummaryWorksheet(XLWorkbook workbook)
    {
        var worksheet = workbook.Worksheets.Add("Summary");

        worksheet.Cell(1, 1).Value = "Metric";
        worksheet.Cell(1, 2).Value = "Value";
        ApplyHeaderStyle(worksheet.Range(1, 1, 1, 2));

        worksheet.Cell(2, 1).Value = "Total Results";
        worksheet.Cell(2, 2).Value = Items.Count;
        worksheet.Cell(3, 1).Value = "Scan Series";
        worksheet.Cell(3, 2).Value = TestSeries.Count;
        worksheet.Cell(4, 1).Value = "Relationship Series";
        worksheet.Cell(4, 2).Value = RelatedSeries.Count;
        worksheet.Cell(5, 1).Value = "Aggregated Series";
        worksheet.Cell(5, 2).Value = AggregatedSeries.Count;
        worksheet.Cell(6, 1).Value = "Saved Relationship Rows";
        worksheet.Cell(6, 2).Value = RelatedSeries.Sum(series => series.Items.Count);
        worksheet.Cell(7, 1).Value = "Scanners With False Negatives";
        worksheet.Cell(7, 2).Value = FalseNegativeParetoSeries.Count;
        worksheet.Cell(8, 1).Value = "Displayed False Negatives";
        worksheet.Cell(8, 2).Value = FalseNegativeParetoSeries
            .SelectMany(series => series.Items)
            .Sum(item => item.FalseNegatives);

        worksheet.Columns().AdjustToContents();
    }

    private static void ApplyHeaderStyle(IXLRange headerRange)
    {
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
    }

    private static void CreateTableIfDataExists(
        IXLWorksheet worksheet,
        string tableName,
        int lastRow,
        int lastColumn)
    {
        if (lastRow < 2)
        {
            return;
        }

        worksheet.Range(1, 1, lastRow, lastColumn)
            .CreateTable(tableName);
    }

    private async Task LoadScanNamesAsync(
        CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory
            .CreateDbContextAsync(cancellationToken);

        _scanNames = await context.Scans
            .AsNoTracking()
            .ToDictionaryAsync(
                scan => scan.Id,
                scan => string.IsNullOrWhiteSpace(scan.Name)
                    ? $"Scan {scan.Id}"
                    : scan.Name,
                cancellationToken);
    }

    private string GetScanName(int scanId)
    {
        return _scanNames.TryGetValue(scanId, out var scanName)
            ? scanName
            : $"Scan {scanId}";
    }

    private string GetScanDisplayName(int scanId)
    {
        return $"{GetScanName(scanId)} ({scanId})";
    }

    private static Axis CreateNumericAxis(
    string name,
    double? minimum = null,
    double? maximum = null,
    Func<double, string>? labeler = null)
    {
        return new Axis
        {
            Name = name,
            MinLimit = minimum,
            MaxLimit = maximum,

            MinStep = 1,

            LabelsPaint = CreateTextPaint(),
            NamePaint = CreateTextPaint(),
            SeparatorsPaint = CreateSeparatorPaint(),

            Labeler = labeler ?? (value => value.ToString("N0")),

            TextSize = 12,

            Padding = new LiveChartsCore.Drawing.Padding(8)
        };
    }

    private static SolidColorPaint CreateTextPaint()
    {
        return new SolidColorPaint(
 new SKColor(120, 120, 120));
    }

    private static SolidColorPaint CreateSeparatorPaint()
    {
        return new SolidColorPaint(
            new SKColor(210, 210, 210))
        {
            StrokeThickness = 1
        };
    }

    private static SKColor GetChartColor(int index)
    {
        return ChartColors[index % ChartColors.Length];
    }
    private static string ToMauiColor(SKColor color)
    {
        return $"#{color.Red:X2}" +
               $"{color.Green:X2}" +
               $"{color.Blue:X2}";
    }

    private static string CoordinateKey(double x, double y)
    {
        return $"{x:F4}|{y:F4}";
    }
}
