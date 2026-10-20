using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Reports.DTO;
using ToolTester.Infrastructure.Services;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.Models.Charts;
using ToolTester.Presentation.Models.Reports;
using ToolTester.Presentation.Services;
using System.Diagnostics;
namespace ToolTester.Presentation.PageModels;

public partial class ReportPageModel : BaseViewModel
{
    private bool _excludeHighVolumeNoise = true;
    private const int HighFalsePositiveThreshold = 10000;
    private const int TestResultPageSize = 250000;
    private const int ReportPageSize = 10000;
    private const int RelatedErrorValue = 0;
    private const int UnrelatedErrorValue = 5;
    private const int ParetoCweLimit = 25;
    private const string AllRelationships = "All Relationships";
    private const string ExcludeUnrelatedRelationships = "Exclude Unrelated";
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

    private IReadOnlyDictionary<int, string> _scanNames =
        new Dictionary<int, string>();

    private ObservableCollection<CweTestResults> _items = [];
    private ObservableCollection<TestSeries> _testSeries = [];
    private ObservableCollection<AggrigatedSeries> _aggregatedSeries = [];
    private ObservableCollection<RelatedSeries> _relatedSeries = [];
   
    private ObservableCollection<RelationshipMixSeries> _relationshipMixSeries = [];
    private ObservableCollection<RelationshipMixChartPoint> _relationshipMixChartData = [];   

    private ObservableCollection<FalseNegativeParetoSeries>
        _falseNegativeParetoSeries = [];

    private ObservableCollection<FalseNegativeParetoSeries>
        _falseNegativeDeclaredParetoSeries = [];

    public ObservableCollection<FalseNegativeParetoSeries>
        FalseNegativeParetoSeries
    {
        get => _falseNegativeParetoSeries;

        private set => SetProperty(
            ref _falseNegativeParetoSeries,
            value);
    }

    public ObservableCollection<FalseNegativeParetoSeries>
        FalseNegativeDeclaredParetoSeries
    {
        get => _falseNegativeDeclaredParetoSeries;

        private set => SetProperty(
            ref _falseNegativeDeclaredParetoSeries,
            value);
    }

    public ObservableCollection<ParetoChartModel>
        GroundTruthParetoCharts
    { get; } = [];

    public ObservableCollection<ParetoChartModel>
        DeclaredCweParetoCharts
    { get; } = [];
    private ObservableCollection<ChartLegendItem> _relationshipMixLegendItems = [];
    private ObservableCollection<ChartLegendItem> _falsePositiveLegendItems = [];
    [ObservableProperty]
    private bool isGroundTruthParetoSelected = true;

    [ObservableProperty]
    private bool isDeclaredCweParetoSelected;

    [ObservableProperty]
    private Color groundTruthTabColor = Colors.DodgerBlue;

    [ObservableProperty]
    private Color declaredCweTabColor = Colors.Gray;

    [RelayCommand]
    private void SelectGroundTruthPareto()
    {
        IsGroundTruthParetoSelected = true;
        IsDeclaredCweParetoSelected = false;

        GroundTruthTabColor = Colors.DodgerBlue;
        DeclaredCweTabColor = Colors.Gray;
    }

    [RelayCommand]
    private void SelectDeclaredCwePareto()
    {
        IsGroundTruthParetoSelected = false;
        IsDeclaredCweParetoSelected = true;

        GroundTruthTabColor = Colors.Gray;
        DeclaredCweTabColor = Colors.DodgerBlue;
    }
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

    private ISeries[] _relationshipMixCountChartSeries = [];
    private Axis[] _relationshipMixCountXAxes = [];
    private Axis[] _relationshipMixCountYAxes = [];

    private ObservableCollection<CweScannerHeatmapPoint>
        _scannerCweHeatmapData = [];

    private ObservableCollection<ChartLegendItem>
    _relatedFindingLegendItems = [];

    public ISeries[] RelationshipMixCountChartSeries
    {
        get => _relationshipMixCountChartSeries;
        private set => SetProperty(
            ref _relationshipMixCountChartSeries,
            value);
    }

    public Axis[] RelationshipMixCountXAxes
    {
        get => _relationshipMixCountXAxes;
        private set => SetProperty(
            ref _relationshipMixCountXAxes,
            value);
    }

    public Axis[] RelationshipMixCountYAxes
    {
        get => _relationshipMixCountYAxes;
        private set => SetProperty(
            ref _relationshipMixCountYAxes,
            value);
    }

    private bool _hasScannerCweHeatmapData;
    public bool ExcludeHighVolumeNoise
    {
        get => _excludeHighVolumeNoise;

        set
        {
            if (!SetProperty(
                    ref _excludeHighVolumeNoise,
                    value))
            {
                return;
            }

            if (!_dataLoaded)
            {
                return;
            }

            RebuildNoiseSensitiveCharts();
        }
    }


    private bool _isNavigatedTo;
    private bool _isLoading;
    private bool _dataLoaded;
    private string _selectedRelationship = AllRelationships;
    private readonly IHeatmapQuery _heatmapQuery;
    private readonly IFalsePositiveQuery _falsePositiveQuery;
    private readonly IReportQueryService _reportQueryService;
    private readonly IScanDisplayNameProvider _scanNameProvider; // if not already present
    private readonly IUnrelatedFindingConsensusService _unrelatedFindingConsensusService;
    public ReportPageModel(
          ModalErrorHandler errorHandler,
          IReportQueryService reportQueryService,
          IScanDisplayNameProvider scanNameProvider,
          BenchmarkReportService benchmarkService,
          IHeatmapQuery heatmapQuery,
          IFalsePositiveQuery falsePositiveQuery,
          IUnrelatedFindingConsensusService unrelatedFindingConsensusService)
    {
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _reportQueryService = reportQueryService ?? throw new ArgumentNullException(nameof(reportQueryService));
        _scanNameProvider = scanNameProvider ?? throw new ArgumentNullException(nameof(scanNameProvider));
        _benchmarkService = benchmarkService ?? throw new ArgumentNullException(nameof(benchmarkService));
        _heatmapQuery = heatmapQuery ?? throw new ArgumentNullException(nameof(heatmapQuery));
        _falsePositiveQuery = falsePositiveQuery;
        _unrelatedFindingConsensusService = unrelatedFindingConsensusService;
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
    private ObservableCollection<UnrelatedFindingConsensus>
    _unrelatedConsensusItems = [];

    private ISeries[] _unrelatedConsensusSeries = [];

    private Axis[] _unrelatedConsensusXAxes = [];

    private Axis[] _unrelatedConsensusYAxes = [];
    public ObservableCollection<UnrelatedFindingConsensus>
    UnrelatedConsensusItems
    {
        get => _unrelatedConsensusItems;

        private set => SetProperty(
            ref _unrelatedConsensusItems,
            value);
    }

    public ISeries[] UnrelatedConsensusSeries
    {
        get => _unrelatedConsensusSeries;

        private set => SetProperty(
            ref _unrelatedConsensusSeries,
            value);
    }

    public Axis[] UnrelatedConsensusXAxes
    {
        get => _unrelatedConsensusXAxes;

        private set => SetProperty(
            ref _unrelatedConsensusXAxes,
            value);
    }

    public Axis[] UnrelatedConsensusYAxes
    {
        get => _unrelatedConsensusYAxes;

        private set => SetProperty(
            ref _unrelatedConsensusYAxes,
            value);
    }

    public bool HasUnrelatedConsensusData
    {
        get => _hasUnrelatedConsensusData;

        private set => SetProperty(
            ref _hasUnrelatedConsensusData,
            value);
    }

    public int UnrelatedConsensusGroupCount =>
        UnrelatedConsensusItems.Count;

    public int UnrelatedConsensusFindingCount =>
        UnrelatedConsensusItems.Sum(
            item => item.FindingCount);
    private bool _hasUnrelatedConsensusData;
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
            var normalized =
                string.IsNullOrWhiteSpace(value)
                    ? AllRelationships
                    : value;

            if (!SetProperty(
                    ref _selectedRelationship,
                    normalized))
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
    ExcludeUnrelatedRelationships,

    .. RelatedSeries
        .SelectMany(series => series.Items)
        .Select(item => item.Relationship)
        .Where(relationship =>
            !string.IsNullOrWhiteSpace(relationship))
        .Distinct(
            StringComparer.OrdinalIgnoreCase)
        .OrderBy(
            relationship => relationship,
            StringComparer.OrdinalIgnoreCase)
 ];

    public async Task InitializeAsync()
    {
        if (_dataLoaded || IsLoading)
        {
            return;
        }
        await _scanNameProvider.LoadAsync();
        await ExecuteLoadAsync();
    }

    public async Task LoadItemsAsync(CancellationToken cancellationToken = default)
    {
        // Ensure the scan name provider is primed
        await _scanNameProvider.LoadAsync(cancellationToken);

        var result = await _reportQueryService.GetReportDataAsync(cancellationToken);

        // assign to VM properties exactly as before
        RelatedSeries = new System.Collections.ObjectModel.ObservableCollection<RelatedSeries>(result.RelatedSeries);
        Items = new System.Collections.ObjectModel.ObservableCollection<CweTestResults>(result.Items);
        TestSeries = new System.Collections.ObjectModel.ObservableCollection<TestSeries>(result.TestSeries);
        AggregatedSeries = new System.Collections.ObjectModel.ObservableCollection<AggrigatedSeries>(result.AggregatedSeries);

        OnPropertyChanged(nameof(AvailableRelationships));

        // original VM still drives subsequent UI-only operations
        BuildRelationshipMix();
    }

    private void BuildFalseNegativePareto(
      IEnumerable<ScannerFalseNegativeDto> data)
    {
        FalseNegativeParetoSeries =
            BuildFalseNegativeParetoSeries(data);
    }

    private void BuildFalseNegativeDeclaredPareto(
        IEnumerable<ScannerFalseNegativeDto> data)
    {
        FalseNegativeDeclaredParetoSeries =
            BuildFalseNegativeParetoSeries(data);
    }

    private ObservableCollection<FalseNegativeParetoSeries>
        BuildFalseNegativeParetoSeries(
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

                /*
                 * Materialize every CWE before taking the displayed top 25.
                 *
                 * The cumulative percentage therefore represents the top CWEs'
                 * contribution to all false negatives for this scan, rather
                 * than being forced to reach 100 percent at item 25.
                 */
                var allMisses = scannerGroup
                    .GroupBy(item => new
                    {
                        item.CweId,
                        item.CweName
                    })
                    .Select(cweGroup => new
                    {
                        cweGroup.Key.CweId,

                        CweName =
                            string.IsNullOrWhiteSpace(
                                cweGroup.Key.CweName)
                                ? $"CWE-{cweGroup.Key.CweId}"
                                : cweGroup.Key.CweName,

                        Opportunities =
                            cweGroup.Sum(item =>
                                item.Opportunities),

                        Detected =
                            cweGroup.Sum(item =>
                                item.Detected),

                        FalseNegatives =
                            cweGroup.Sum(item =>
                                item.FalseNegatives)
                    })
                    .Where(item =>
                        item.FalseNegatives > 0)
                    .OrderByDescending(item =>
                        item.FalseNegatives)
                    .ThenBy(item =>
                        item.CweId)
                    .ToList();

                var totalFalseNegatives =
                    allMisses.Sum(item =>
                        item.FalseNegatives);

                var topMisses = allMisses
                    .Take(ParetoCweLimit)
                    .ToList();

                var runningFalseNegatives = 0;

                var points = topMisses
                    .Select(item =>
                    {
                        runningFalseNegatives +=
                            item.FalseNegatives;

                        var cumulativePercent =
                            totalFalseNegatives <= 0
                                ? 0d
                                : runningFalseNegatives *
                                  100d /
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
                            FalseNegatives =
                                item.FalseNegatives,
                            CumulativePercent =
                                cumulativePercent
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
            .Where(series =>
                series.Items.Count > 0)
            .ToList();

        return new ObservableCollection<
            FalseNegativeParetoSeries>(
            scannerSeries);
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

            var relationshipCounts = relatedSeries.Items
      .Where(item =>
          !string.IsNullOrWhiteSpace(
              item.Relationship))
      .Where(item =>
          !IsExcludedHighVolumeUnrelatedResult(
              scanId,
              item.ScannerCweId,
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

    private async Task BuildLiveCharts()
    {
        var falseNegatives = await _benchmarkService
             .GetFalseNegativesByScannerAsync();

        BuildFalseNegativePareto(falseNegatives);

        BuildMitreTop25Chart(falseNegatives);
        var declaredCweFalseNegatives =
                await _benchmarkService
                    .GetFalseNegativesByScannerDeclaredAsync();

        BuildFalseNegativeDeclaredPareto(
            declaredCweFalseNegatives);

        /*
         * Both Pareto source collections now exist, so build
         * their LiveCharts models immediately.
         */
        BuildParetoCharts();
        await BuildHeapChart();

        await BuildFalsePositiveChart();
        await BuildUnrelatedConsensusChart();
        BuildRelatedFindingChart();
        BuildParetoCharts();
        BuildRelationshipMixLiveChart();
        BuildRelationshipMixCountLiveChart();
        BuildPolarLiveChart();
    }
    private async Task BuildUnrelatedConsensusChart()
    {
        var consensus =
            await _unrelatedFindingConsensusService
                .GetConsensusAsync(
                    minimumScannerCount: 2);

        var items =
            consensus
                .OrderBy(item =>
                    item.GroundTruthCweId)
                .ThenBy(item =>
                    item.ScannerFoundCweId)
                .ToArray();

        UnrelatedConsensusItems =
            new ObservableCollection<
                UnrelatedFindingConsensus>(
                    items);

        OnPropertyChanged(
            nameof(UnrelatedConsensusGroupCount));

        OnPropertyChanged(
            nameof(UnrelatedConsensusFindingCount));

        if (items.Length == 0)
        {
            UnrelatedConsensusSeries = [];
            UnrelatedConsensusXAxes = [];
            UnrelatedConsensusYAxes = [];
            HasUnrelatedConsensusData = false;

            return;
        }

        var chartSeries =
            new List<ISeries>();

        /*
         * Create a separate series for each consensus level.
         *
         * Example:
         *
         * 2 scanners
         * 3 scanners
         * 4 scanners
         *
         * This gives each consensus level its own color.
         */
        var consensusGroups =
            items
                .GroupBy(item =>
                    item.ScannerCount)
                .OrderBy(group =>
                    group.Key)
                .ToArray();
#if DEBUG
        DebugUnrelatedConsensus(consensus);
#endif
        var colorIndex = 0;

        foreach (var consensusGroup in consensusGroups)
        {
            var scannerCount =
                consensusGroup.Key;

            var groupItems =
                consensusGroup.ToArray();

            /*
             * Coordinate lookup is used by the tooltip.
             *
             * X = Ground Truth CWE
             * Y = Scanner CWE
             */
            var lookup =
                groupItems
                    .GroupBy(item =>
                        CoordinateKey(
                            item.GroundTruthCweId,
                            item.ScannerFoundCweId))
                    .ToDictionary(
                        group => group.Key,
                        group => group.First());

            var color =
                GetChartColor(
                    colorIndex++);

            var series =
                new ScatterSeries<WeightedPoint>
                {
                    Name =
                        $"{scannerCount} Scanners",

                    Values =
                        groupItems
                            .Select(item =>
                                new WeightedPoint(
                                    item.GroundTruthCweId,
                                    item.ScannerFoundCweId,

                                    /*
                                     * Bubble weight represents the
                                     * number of findings.
                                     */
                                    Math.Max(
                                        item.FindingCount,
                                        1)))
                            .ToArray(),

                    IsVisible =
                        true,

                    IsVisibleAtLegend =
                        true,

                    MinGeometrySize =
                        8,

                    GeometrySize =
                        35,

                    Fill =
                        new SolidColorPaint(
                            color.WithAlpha(150)),

                    Stroke =
                        new SolidColorPaint(
                            color)
                        {
                            StrokeThickness = 1
                        },

                    YToolTipLabelFormatter =
                        point =>
                        {
                            var key =
                                CoordinateKey(
                                    point.Coordinate
                                        .SecondaryValue,
                                    point.Coordinate
                                        .PrimaryValue);

                            if (!lookup.TryGetValue(
                                    key,
                                    out var item))
                            {
                                return
                                    $"{scannerCount} scanners";
                            }

                            return
                                $"Ground Truth CWE: " +
                                $"CWE-{item.GroundTruthCweId}" +
                                $"{Environment.NewLine}" +

                                $"Scanner CWE: " +
                                $"CWE-{item.ScannerFoundCweId}" +
                                $"{Environment.NewLine}" +

                                $"Consensus: " +
                                $"{item.ScannerCount} scanners" +
                                $"{Environment.NewLine}" +

                                $"Scanners: " +
                                $"{item.ScannerNamesLabel}" +
                                $"{Environment.NewLine}" +

                                $"Scans: " +
                                $"{item.ScanCount:N0}" +
                                $"{Environment.NewLine}" +

                                $"Findings: " +
                                $"{item.FindingCount:N0}";
                        }
                };

            chartSeries.Add(
                series);
        }

        UnrelatedConsensusSeries =
            chartSeries.ToArray();

        UnrelatedConsensusXAxes =
        [
            CreateNumericAxis(
            name:
                "Ground Truth CWE",

            labeler:
                value =>
                    $"CWE-{value:N0}")
        ];

        UnrelatedConsensusYAxes =
        [
            CreateNumericAxis(
            name:
                "Scanner CWE",

            labeler:
                value =>
                    $"CWE-{value:N0}")
        ];

        HasUnrelatedConsensusData =
            chartSeries.Count > 0;
    }
    private static void DebugUnrelatedConsensus(
    IEnumerable<UnrelatedFindingConsensus> consensus)
    {
        var items =
            consensus
                .OrderByDescending(item =>
                    item.ScannerCount)
                .ThenByDescending(item =>
                    item.FindingCount)
                .ThenBy(item =>
                    item.GroundTruthCweId)
                .ThenBy(item =>
                    item.ScannerFoundCweId)
                .ToArray();

        Debug.WriteLine(
            "============================================================");

        Debug.WriteLine(
            "UNRELATED SCANNER CONSENSUS - RELATIONSHIP REVIEW");

        Debug.WriteLine(
            "============================================================");

        Debug.WriteLine(
            $"Consensus groups: {items.Length}");

        Debug.WriteLine(string.Empty);

        foreach (var item in items)
        {
            Debug.WriteLine(
                $"Ground Truth CWE-{item.GroundTruthCweId} | " +
                $"Scanner CWE-{item.ScannerFoundCweId} | " +
                $"Scanners={item.ScannerCount} | " +
                $"Findings={item.FindingCount} | " +
                $"Scans={item.ScanCount}");

            foreach (var example in item.Examples)
            {
                Debug.WriteLine(
                    $"  Scanner: {example.ScannerName}");

                Debug.WriteLine(
                    $"    ScanId: {example.ScanId}");

                Debug.WriteLine(
                    $"    TestId: {example.TestId}");

                Debug.WriteLine(
                    $"    File: " +
                    $"{example.FilePath ?? "<null>"}");

                Debug.WriteLine(
                    $"    Line: " +
                    $"{example.Line?.ToString() ?? "<null>"}");

                Debug.WriteLine(
                    $"    Rule: " +
                    $"{DisplayDebugValue(example.RuleId)}");

                Debug.WriteLine(
                    $"    Title: " +
                    $"{DisplayDebugValue(example.Title)}");
            }

            Debug.WriteLine(string.Empty);
        }

        Debug.WriteLine(
            "============================================================");
    }

    private static string DisplayDebugValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "<empty>"
            : value;
    }
    private async Task BuildHeapChart()
    {
        var heatmapDto = await _heatmapQuery.GetHeatmapAsync();
        var heatmapResult = heatmapDto.ToHeatmapResult(); // uses mapper above

        ScannerCweHeatmapData = heatmapResult.Data;
        ScannerCweHeatmapSeries = heatmapResult.Series;
        ScannerCweHeatmapXAxes = heatmapResult.XAxes;
        ScannerCweHeatmapYAxes = heatmapResult.YAxes;
        HasScannerCweHeatmapData = heatmapResult.HasData;
    }

    private async Task BuildFalsePositiveChart()
    {
        // example: call inside BuildLiveCharts or Initialize
        var fpDto = await _falsePositiveQuery.GetFalsePositiveChartAsync();
        var fpResult = fpDto.ToFalsePositiveResult();

        FalsePositiveSeries = fpResult.Series;
        FalsePositiveXAxes = fpResult.XAxes;
        FalsePositiveYAxes = fpResult.YAxes;

        FalsePositiveLegendItems = new ObservableCollection<ChartLegendItem>(
      fpResult.LegendInfo.Select((info, idx) =>
          new ChartLegendItem(
              fpResult.Series.ElementAtOrDefault(idx) ?? throw new InvalidOperationException($"No series for legend index {idx}"),
              ToMauiColor(info.Color),
              info.IsVisible)
          {
              // optional init
          }));
    }
    private IEnumerable<RelatedItemsInTest> ApplyRelationshipFilter(
    IEnumerable<RelatedItemsInTest> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (string.Equals(
                SelectedRelationship,
                AllRelationships,
                StringComparison.OrdinalIgnoreCase))
        {
            return source;
        }

        if (string.Equals(
                SelectedRelationship,
                ExcludeUnrelatedRelationships,
                StringComparison.OrdinalIgnoreCase))
        {
            return source.Where(item =>
                !string.Equals(
                    item.Relationship,
                    nameof(CweRelationshipKind.Unrelated),
                    StringComparison.OrdinalIgnoreCase));
        }

        return source.Where(item =>
            string.Equals(
                item.Relationship,
                SelectedRelationship,
                StringComparison.OrdinalIgnoreCase));
    }

    private void BuildRelatedFindingChart()
    {
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
            var filteredItems =
      ApplyRelationshipFilter(
              dataSeries.Items)
          .ToList();

            /*
             * Keep the scanner series visible.
             *
             * Remove only a specific ScanId + ScannerCweId result when
             * that scanner/CWE count exceeds the configured threshold.
             */
            var plottableItems = filteredItems
     .Where(item =>
         item.ScannerCweId.HasValue &&
         !IsExcludedHighVolumeUnrelatedResult(
             dataSeries.ScanId,
             item.ScannerCweId,
             item.Relationship))
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

            var color =
                GetChartColor(colorIndex++);

            var seriesName =
                GetScanDisplayName(
                    dataSeries.ScanId);

            /*
             * Do not use the high false-positive condition here.
             * The scanner remains checked and visible.
             */
            var isVisible =
                !previousVisibility.TryGetValue(
                    seriesName,
                    out var previousValue) ||
                previousValue;

            var series =
                new ScatterSeries<WeightedPoint>
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

                    Fill =
                        new SolidColorPaint(
                            color.WithAlpha(150)),

                    Stroke =
                        new SolidColorPaint(color)
                        {
                            StrokeThickness = 1
                        },

                    YToolTipLabelFormatter =
                        point =>
                        {
                            var key = CoordinateKey(
                                point.Coordinate.SecondaryValue,
                                point.Coordinate.PrimaryValue);

                            if (!lookup.TryGetValue(
                                    key,
                                    out var item))
                            {
                                return $"Scan: {seriesName}";
                            }

                            return
                                $"Scan: {seriesName}" +
                                $"{Environment.NewLine}" +
                                $"Ground Truth CWE: " +
                                $"{item.GroundTruthCweId}" +
                                $"{Environment.NewLine}" +
                                $"Scanner CWE: " +
                                $"{item.ScannerCweId!.Value}" +
                                $"{Environment.NewLine}" +
                                $"Relationship: " +
                                $"{item.Relationship}" +
                                $"{Environment.NewLine}" +
                                $"Confidence: " +
                                $"{item.RelationshipScore}" +
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
            labeler: value =>
                $"CWE-{value:N0}")
        ];

        RelatedFindingYAxes =
        [
            CreateNumericAxis(
            name: "Scanner CWE",
            labeler: value =>
                $"CWE-{value:N0}")
        ];
    }
    private void BuildParetoCharts()
    {
        GroundTruthParetoCharts.Clear();
        DeclaredCweParetoCharts.Clear();

        AppendParetoChartsFromSeries(
            seriesCollection:
                FalseNegativeParetoSeries,
            titleSuffix:
                " - Ground Truth",
            cweAxisName:
                "Juliet Ground-Truth CWE",
            cweTooltipName:
                "Ground Truth CWE",
            charts:
                GroundTruthParetoCharts);

        AppendParetoChartsFromSeries(
            seriesCollection:
                FalseNegativeDeclaredParetoSeries,
            titleSuffix:
                " - Declared CWE",
            cweAxisName:
                "Scanner-Declared CWE",
            cweTooltipName:
                "Declared CWE",
            charts:
                DeclaredCweParetoCharts);
    }
    private void AppendParetoChartsFromSeries(
    IEnumerable<FalseNegativeParetoSeries>?
        seriesCollection,
    string titleSuffix,
    string cweAxisName,
    string cweTooltipName,
    ObservableCollection<ParetoChartModel> charts)
    {
        ArgumentNullException.ThrowIfNull(charts);

        if (seriesCollection is null)
        {
            return;
        }

        foreach (var scannerSeries in seriesCollection)
        {
            if (scannerSeries.Items is null)
            {
                continue;
            }

            var items = scannerSeries.Items
                .Where(item =>
                    item.FalseNegatives > 0)
                .OrderByDescending(item =>
                    item.FalseNegatives)
                .ThenBy(item =>
                    item.CweId)
                .ToList();

            if (items.Count == 0)
            {
                continue;
            }

            var itemLookup = items
                .Select((item, itemIndex) => new
                {
                    ItemIndex = itemIndex,
                    Item = item
                })
                .ToDictionary(
                    entry => entry.ItemIndex,
                    entry => entry.Item);

            var falseNegativeValues = items
                .Select((item, itemIndex) =>
                    new ObservablePoint(
                        itemIndex,
                        item.FalseNegatives))
                .ToArray();

            var cumulativeValues = items
                .Select((item, itemIndex) =>
                    new ObservablePoint(
                        itemIndex,
                        item.CumulativePercent))
                .ToArray();

            var falseNegativeSeries =
                new ColumnSeries<ObservablePoint>
                {
                    Name = "False Negatives",
                    Values = falseNegativeValues,
                    ScalesYAt = 0,
                    Fill =
                        new SolidColorPaint(
                            SKColors.SteelBlue),
                    Stroke = null,
                    MaxBarWidth = 45,
                    DataLabelsPaint =
                        new SolidColorPaint(
                            SKColors.DarkGray),
                    DataLabelsPosition =
                        DataLabelsPosition.Top,
                    DataLabelsFormatter = point =>
                        point.Coordinate.PrimaryValue
                            .ToString("N0"),
                    XToolTipLabelFormatter = point =>
                    {
                        var itemIndex =
                            Convert.ToInt32(
                                Math.Round(
                                    point.Coordinate
                                        .SecondaryValue));

                        if (!itemLookup.TryGetValue(
                                itemIndex,
                                out var item))
                        {
                            return
                                $"False Negatives: " +
                                $"{point.Coordinate.PrimaryValue:N0}";
                        }

                        return
                            $"Scanner: {item.ScannerName}" +
                            $"{Environment.NewLine}" +
                            $"{cweTooltipName}: " +
                            $"{item.CweLabel}" +
                            $"{Environment.NewLine}" +
                            $"CWE Name: {item.CweName}" +
                            $"{Environment.NewLine}" +
                            $"Known Opportunities: " +
                            $"{item.Opportunities:N0}" +
                            $"{Environment.NewLine}" +
                            $"Detected: {item.Detected:N0}" +
                            $"{Environment.NewLine}" +
                            $"False Negatives: " +
                            $"{item.FalseNegatives:N0}";
                    }
                };

            var cumulativeSeries =
                new LineSeries<ObservablePoint>
                {
                    Name = "Cumulative %",
                    Values = cumulativeValues,
                    ScalesYAt = 1,
                    Fill = null,
                    LineSmoothness = 0,
                    Stroke =
                        new SolidColorPaint(
                            SKColors.DarkOrange)
                        {
                            StrokeThickness = 3
                        },
                    GeometryFill =
                        new SolidColorPaint(
                            SKColors.DarkOrange),
                    GeometryStroke =
                        new SolidColorPaint(
                            SKColors.White)
                        {
                            StrokeThickness = 2
                        },
                    GeometrySize = 10,
                    XToolTipLabelFormatter = point =>
                    {
                        var itemIndex =
                            Convert.ToInt32(
                                Math.Round(
                                    point.Coordinate
                                        .SecondaryValue));

                        if (!itemLookup.TryGetValue(
                                itemIndex,
                                out var item))
                        {
                            return
                                $"Cumulative: " +
                                $"{point.Coordinate.PrimaryValue:F2}%";
                        }

                        return
                            $"Scanner: {item.ScannerName}" +
                            $"{Environment.NewLine}" +
                            $"{cweTooltipName}: " +
                            $"{item.CweLabel}" +
                            $"{Environment.NewLine}" +
                            $"Cumulative: " +
                            $"{item.CumulativePercent:F2}%";
                    }
                };

            var xAxis = new Axis
            {
                Name = cweAxisName,
                Labels = items
                    .Select(item =>
                        item.CweLabel)
                    .ToArray(),
                LabelsRotation = -45,
                MinLimit = -0.5,
                MaxLimit = items.Count - 0.5,
                MinStep = 1,
                ForceStepToMin = true,
                LabelsPaint = CreateTextPaint(),
                NamePaint = CreateTextPaint(),
                SeparatorsPaint = null
            };

            var falseNegativeAxis = new Axis
            {
                Name = "False Negatives",
                MinLimit = 0,
                LabelsPaint = CreateTextPaint(),
                NamePaint = CreateTextPaint(),
                SeparatorsPaint =
                    CreateSeparatorPaint(),
                Labeler = value =>
                    value.ToString("N0")
            };

            var cumulativeAxis = new Axis
            {
                Name = "Cumulative Percentage",
                Position = AxisPosition.End,
                MinLimit = 0,
                MaxLimit = 100,
                MinStep = 20,
                ForceStepToMin = true,
                LabelsPaint =
                    new SolidColorPaint(
                        SKColors.DarkOrange),
                NamePaint =
                    new SolidColorPaint(
                        SKColors.DarkOrange),
                SeparatorsPaint = null,
                Labeler = value =>
                    $"{value:F0}%"
            };

            charts.Add(
                new ParetoChartModel
                {
                    Title =
                        $"False Negatives Pareto" +
                        $"{titleSuffix} - " +
                        $"{scannerSeries.ScannerName}",

                    Series =
                    [
                        falseNegativeSeries,
                    cumulativeSeries
                    ],

                    XAxes =
                    [
                        xAxis
                    ],

                    YAxes =
                    [
                        falseNegativeAxis,
                    cumulativeAxis
                    ]
                });
        }
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

        const string finalTotalLabel = "Final Total";

        /*
         * Snapshot the data so every lookup in this chart is based on the
         * same collection state.
         */
        var chartData =
            RelationshipMixChartData
                .Where(item =>
                    !string.IsNullOrWhiteSpace(item.ScanName) &&
                    !string.IsNullOrWhiteSpace(item.Relationship))
                .ToArray();

        /*
         * These are the individual scanner/scan names displayed before the
         * aggregate Final Total column.
         */
        var individualScannerNames =
            chartData
                .Select(item => item.ScanName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    name => name,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        /*
         * scannerNames defines the X-axis and the position of every value
         * in every stacked series.
         */
        var scannerNames =
            individualScannerNames
                .Append(finalTotalLabel)
                .ToArray();

        /*
         * Calculate the total count for each specific scanner.
         */
        var scannerTotals =
            chartData
                .GroupBy(
                    item => item.ScanName,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(item => item.Count),
                    StringComparer.OrdinalIgnoreCase);

        /*
         * Grand total across every scanner and every relationship.
         */
        var finalTotalCount =
            scannerTotals.Values.Sum();

        /*
         * Group scanner/relationship records once. This safely handles
         * duplicate data points for the same scanner and relationship.
         */
        var scannerRelationshipGroups =
            chartData
                .GroupBy(
                    item => (
                        ScanName: item.ScanName.ToUpperInvariant(),
                        Relationship: item.Relationship.ToUpperInvariant()))
                .ToDictionary(
                    group => group.Key,
                    group => group.ToArray());

        RelationshipMixXAxes =
        [
            new Axis
        {
            Name = "Scanner",

            /*
             * Use scannerNames, not individualScannerNames, because the
             * series includes the Final Total value.
             */
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
             * Aggregate this relationship across every scanner.
             */
            var relationshipFinalCount =
                chartData
                    .Where(point =>
                        string.Equals(
                            point.Relationship,
                            definition.Relationship,
                            StringComparison.OrdinalIgnoreCase))
                    .Sum(point => point.Count);

            var relationshipFinalPercentage =
                finalTotalCount > 0
                    ? relationshipFinalCount * 100d / finalTotalCount
                    : 0d;

            /*
             * Build one percentage value for every scanner, followed by the
             * overall Final Total percentage.
             */
            var values =
                scannerNames
                    .Select(scannerName =>
                    {
                        if (string.Equals(
                                scannerName,
                                finalTotalLabel,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            return relationshipFinalPercentage;
                        }

                        if (!scannerTotals.TryGetValue(
                                scannerName,
                                out var scannerTotalCount) ||
                            scannerTotalCount <= 0)
                        {
                            return 0d;
                        }

                        var lookupKey =
                        (
                            ScanName: scannerName.ToUpperInvariant(),
                            Relationship:
                                definition.Relationship.ToUpperInvariant()
                        );

                        if (!scannerRelationshipGroups.TryGetValue(
                                lookupKey,
                                out var matchingPoints))
                        {
                            return 0d;
                        }

                        var relationshipCount =
                            matchingPoints.Sum(point => point.Count);

                        return
                            relationshipCount * 100d /
                            scannerTotalCount;
                    })
                    .ToArray();

            /*
             * Do not create a series when the relationship has no values for
             * any scanner or for the aggregate total.
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
                     * Keep disabled series in the custom legend.
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
                     * SecondaryValue is the X-axis position. scannerNames is
                     * used for both the labels and series values, so the
                     * position maps directly to the correct scanner.
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
                $"{definition.Relationship}: " +
                $"{point.Coordinate.PrimaryValue:F2}%";
        }

        var scanName =
            scannerNames[scannerIndex];

        /*
         * Aggregate Final Total column.
         */
        if (string.Equals(
                scanName,
                finalTotalLabel,
                StringComparison.OrdinalIgnoreCase))
        {
            return
                $"{definition.Relationship}: " +
                $"{relationshipFinalCount:N0} / " +
                $"{finalTotalCount:N0} " +
                $"({relationshipFinalPercentage:F2}%)";
        }

        var scannerTotalCount =
            scannerTotals.TryGetValue(
                scanName,
                out var scannerTotal)
                ? scannerTotal
                : 0;

        var lookupKey =
        (
            ScanName: scanName.ToUpperInvariant(),
            Relationship:
                definition.Relationship.ToUpperInvariant()
        );

        scannerRelationshipGroups.TryGetValue(
            lookupKey,
            out var matchingPoints);

        var relationshipCount =
            matchingPoints?.Sum(item => item.Count) ?? 0;

        var percentage =
            scannerTotalCount > 0
                ? relationshipCount * 100d /
                  scannerTotalCount
                : 0d;

        /*
         * LiveCharts invokes this formatter once for each
         * relationship segment. Return exactly one line
         * for the segment.
         *
         * The scanner name is already shown in the shared
         * tooltip header, so it does not need to be repeated.
         */
        return
            $"{definition.Relationship}: " +
            $"{relationshipCount:N0} / " +
            $"{scannerTotalCount:N0} " +
            $"({percentage:F2}%)";
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
    private void BuildRelationshipMixCountLiveChart()
    {
        const string finalTotalLabel = "Final Total";

        /*
         * IMPORTANT:
         *
         * This count chart intentionally uses RelatedSeries directly.
         *
         * It does NOT use RelationshipMixChartData because that collection
         * has already had the high-volume scanner/CWE results excluded.
         *
         * Therefore:
         *
         *   Percentage Relationship Mix = noise-filtered
         *   Count Relationship Mix      = includes ALL findings
         *
         * This lets the count chart expose the high-volume CWE results
         * rather than hiding them.
         */

        var chartData = RelatedSeries
            .SelectMany(series =>
                series.Items
                    .Where(item =>
                        !string.IsNullOrWhiteSpace(
                            item.Relationship))
                    .Select(item => new
                    {
                        series.ScanId,

                        ScanName =
                            GetScanName(series.ScanId),

                        ScannerName =
                            GetScanDisplayName(series.ScanId),

                        item.ScannerCweId,

                        item.GroundTruthCweId,

                        item.Relationship,

                        item.Count,

                        IsHighVolume =
                            item.ScannerCweId.HasValue &&
                            IsExcludedHighVolumeResult(
                                series.ScanId,
                                item.ScannerCweId.Value)
                    }))
            .ToArray();

        if (chartData.Length == 0)
        {
            RelationshipMixCountChartSeries = [];
            RelationshipMixCountXAxes = [];
            RelationshipMixCountYAxes = [];

            return;
        }

        var individualScannerNames = chartData
            .Select(item => item.ScanName)
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(
                name => name,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var scannerNames = individualScannerNames
            .Append(finalTotalLabel)
            .ToArray();

        /*
         * ALL findings are included here, including results identified
         * as high-volume/noise by IsHighFalsePositiveResult().
         */
        var scannerTotals = chartData
            .GroupBy(
                item => item.ScanName,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(
                    item => item.Count),
                StringComparer.OrdinalIgnoreCase);

        var finalTotalCount =
            scannerTotals.Values.Sum();

        /*
         * Aggregate relationship counts for every scanner.
         *
         * No high-volume exclusion is applied.
         */
        var scannerRelationshipGroups = chartData
            .GroupBy(
                item => (
                    ScanName:
                        item.ScanName.ToUpperInvariant(),

                    Relationship:
                        item.Relationship.ToUpperInvariant()))
            .ToDictionary(
                group => group.Key,
                group => group.ToArray());

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

        foreach (var definition in definitions)
        {
            /*
             * Overall relationship count INCLUDING high-volume results.
             */
            var relationshipFinalCount = chartData
                .Where(item =>
                    string.Equals(
                        item.Relationship,
                        definition.Relationship,
                        StringComparison.OrdinalIgnoreCase))
                .Sum(item => item.Count);

            var values = scannerNames
                .Select(scannerName =>
                {
                    if (string.Equals(
                            scannerName,
                            finalTotalLabel,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return (double)relationshipFinalCount;
                    }

                    var lookupKey =
                    (
                        ScanName:
                            scannerName.ToUpperInvariant(),

                        Relationship:
                            definition.Relationship.ToUpperInvariant()
                    );

                    if (!scannerRelationshipGroups.TryGetValue(
                            lookupKey,
                            out var matchingPoints))
                    {
                        return 0d;
                    }

                    return (double)matchingPoints.Sum(
                        point => point.Count);
                })
                .ToArray();

            if (values.All(value => value <= 0))
            {
                continue;
            }

            /*
             * Match the current visibility state of the percentage chart's
             * relationship legend where possible.
             */
            var existingLegendItem =
                RelationshipMixLegendItems.FirstOrDefault(
                    item => string.Equals(
                        item.Name,
                        definition.Relationship,
                        StringComparison.OrdinalIgnoreCase));

            var isVisible =
                existingLegendItem?.IsVisible ?? true;

            var series =
                new StackedColumnSeries<double>
                {
                    Name =
                        definition.Relationship,

                    Values =
                        values,

                    IsVisible =
                        isVisible,

                    IsVisibleAtLegend =
                        true,

                    Fill =
                        new SolidColorPaint(
                            definition.Color),

                    Stroke =
                        null,

                    MaxBarWidth =
                        65,

                    YToolTipLabelFormatter =
                        point =>
                        {
                            var scannerIndex =
                                Convert.ToInt32(
                                    Math.Round(
                                        point.Coordinate
                                            .SecondaryValue));

                            if (scannerIndex < 0 ||
                                scannerIndex >=
                                scannerNames.Length)
                            {
                                return
                                    $"Relationship: " +
                                    $"{definition.Relationship}" +
                                    $"{Environment.NewLine}" +
                                    $"Count: " +
                                    $"{point.Coordinate.PrimaryValue:N0}";
                            }

                            var scanName =
                                scannerNames[
                                    scannerIndex];

                            if (string.Equals(
                                    scanName,
                                    finalTotalLabel,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                var finalHighVolumeCount =
                                    chartData
                                        .Where(item =>
                                            item.IsHighVolume &&
                                            string.Equals(
                                                item.Relationship,
                                                definition.Relationship,
                                                StringComparison.OrdinalIgnoreCase))
                                        .Sum(item =>
                                            item.Count);

                                return
                                    $"Scanner: {finalTotalLabel}" +
                                    $"{Environment.NewLine}" +
                                    $"Relationship: " +
                                    $"{definition.Relationship}" +
                                    $"{Environment.NewLine}" +
                                    $"Relationship Count: " +
                                    $"{relationshipFinalCount:N0}" +
                                    $"{Environment.NewLine}" +
                                    $"High-volume Count: " +
                                    $"{finalHighVolumeCount:N0}" +
                                    $"{Environment.NewLine}" +
                                    $"All Findings: " +
                                    $"{finalTotalCount:N0}";
                            }

                            var lookupKey =
                            (
                                ScanName:
                                    scanName.ToUpperInvariant(),

                                Relationship:
                                    definition.Relationship
                                        .ToUpperInvariant()
                            );

                            scannerRelationshipGroups
                                .TryGetValue(
                                    lookupKey,
                                    out var matchingPoints);

                            var relationshipCount =
                                matchingPoints?.Sum(
                                    item => item.Count) ?? 0;

                            var highVolumeCount =
                                matchingPoints?
                                    .Where(item =>
                                        item.IsHighVolume)
                                    .Sum(item =>
                                        item.Count) ?? 0;

                            var scannerTotal =
                                scannerTotals.TryGetValue(
                                    scanName,
                                    out var total)
                                    ? total
                                    : 0;

                            var displayName =
                                matchingPoints?
                                    .FirstOrDefault()?
                                    .ScannerName
                                ?? scanName;

                            return
                                $"Scanner: {displayName}" +
                                $"{Environment.NewLine}" +
                                $"Relationship: " +
                                $"{definition.Relationship}" +
                                $"{Environment.NewLine}" +
                                $"Relationship Count: " +
                                $"{relationshipCount:N0}" +
                                $"{Environment.NewLine}" +
                                $"High-volume Count: " +
                                $"{highVolumeCount:N0}" +
                                $"{Environment.NewLine}" +
                                $"Scanner Total: " +
                                $"{scannerTotal:N0}";
                        }
                };

            chartSeries.Add(series);
        }

        RelationshipMixCountChartSeries =
            chartSeries.ToArray();

        RelationshipMixCountXAxes =
        [
            new Axis
        {
            Name =
                "Scanner",

            Labels =
                scannerNames,

            MinLimit =
                -0.5,

            MaxLimit =
                scannerNames.Length - 0.5,

            MinStep =
                1,

            ForceStepToMin =
                true,

            LabelsRotation =
                -45,

            LabelsPaint =
                CreateTextPaint(),

            NamePaint =
                CreateTextPaint(),

            SeparatorsPaint =
                null
        }
        ];

        RelationshipMixCountYAxes =
        [
            new Axis
        {
            Name =
                "Finding Count",

            MinLimit =
                0,

            LabelsPaint =
                CreateTextPaint(),

            NamePaint =
                CreateTextPaint(),

            SeparatorsPaint =
                CreateSeparatorPaint(),

            Labeler =
                value =>
                    value.ToString("N0")
        }
        ];
    }
    private void BuildPolarLiveChart()
    {
        var chartSeries =
            new List<ISeries>();

        var index = 0;

        foreach (var dataSeries in AggregatedSeries
                     .OrderBy(series => series.ScanId))
        {
            /*
             * Remove only individual excessive ScanId + CWE results.
             * Do not hide the complete scanner series.
             */
            var plottableItems = dataSeries.Items
                .Where(item =>
                    !IsExcludedHighVolumeResult(
                        dataSeries.ScanId,
                        item.CweId))
                .OrderBy(item => item.CweId)
                .ToArray();

            if (plottableItems.Length == 0)
            {
                continue;
            }

            var color =
                GetChartColor(index++);

            chartSeries.Add(
                new PolarLineSeries<ObservablePoint>
                {
                    Name =
                        GetScanDisplayName(
                            dataSeries.ScanId),

                    Values = plottableItems
                        .Select(item =>
                            new ObservablePoint(
                                item.CweId,
                                item.Count))
                        .ToArray(),

                    IsVisible = true,

                    IsVisibleAtLegend = true,

                    IsClosed = true,

                    LineSmoothness = 0,

                    Fill =
                        new SolidColorPaint(
                            color.WithAlpha(50)),

                    Stroke =
                        new SolidColorPaint(color)
                        {
                            StrokeThickness = 2
                        },

                    GeometryFill =
                        new SolidColorPaint(color),

                    GeometryStroke =
                        new SolidColorPaint(
                            SKColors.White)
                        {
                            StrokeThickness = 1
                        },

                    GeometrySize = 8
                });
        }

        PolarSeries =
            chartSeries.ToArray();

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

        FalseNegativeParetoSeries.Clear();
        FalseNegativeDeclaredParetoSeries.Clear();

        GroundTruthParetoCharts.Clear();
        DeclaredCweParetoCharts.Clear();

        FalsePositiveLegendItems.Clear();
        RelatedFindingLegendItems.Clear();
        RelationshipMixLegendItems.Clear();

        RelationshipMixSeries.Clear();
        RelationshipMixChartData.Clear();

        FalsePositiveSeries = [];
        FalsePositiveXAxes = [];
        FalsePositiveYAxes = [];

        RelatedFindingSeries = [];
        RelatedFindingXAxes = [];
        RelatedFindingYAxes = [];

        RelationshipMixChartSeries = [];
        RelationshipMixXAxes = [];
        RelationshipMixYAxes = [];

        RelationshipMixCountChartSeries = [];
        RelationshipMixCountXAxes = [];
        RelationshipMixCountYAxes = [];

        PolarSeries = [];
        PolarAngleAxes = [];
        PolarRadiusAxes = [];

        MitreTop25Series = [];
        MitreTop25XAxes = [];
        MitreTop25YAxes = [];
        HasMitreTop25Data = false;

        ScannerCweHeatmapData.Clear();
        ScannerCweHeatmapSeries = [];
        ScannerCweHeatmapXAxes = [];
        ScannerCweHeatmapYAxes = [];
        HasScannerCweHeatmapData = false;

        IsGroundTruthParetoSelected = true;
        IsDeclaredCweParetoSelected = false;

        GroundTruthTabColor = Colors.DodgerBlue;
        DeclaredCweTabColor = Colors.Gray;
        UnrelatedConsensusItems.Clear();

        UnrelatedConsensusSeries = [];

        UnrelatedConsensusXAxes = [];

        UnrelatedConsensusYAxes = [];

        HasUnrelatedConsensusData = false;

        OnPropertyChanged(
            nameof(UnrelatedConsensusGroupCount));

        OnPropertyChanged(
            nameof(UnrelatedConsensusFindingCount));
        OnPropertyChanged(
            nameof(HasLoadedData));

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



            await BuildLiveCharts();


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

    private string GetScanName(int scanId) =>
     _scanNameProvider.GetScanName(scanId);

    private string GetScanDisplayName(int scanId) =>
        _scanNameProvider.GetScanDisplayName(scanId);

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
    private void RebuildNoiseSensitiveCharts()
    {
        /*
         * RelationshipMixChartData must be rebuilt first because
         * the percentage relationship chart uses that collection.
         */
        BuildRelationshipMix();

        BuildRelatedFindingChart();

        BuildRelationshipMixLiveChart();

        BuildPolarLiveChart();

        /*
         * Do NOT rebuild/filter the raw count relationship chart here
         * if the intent is for that chart to always show the original
         * counts, including high-volume findings.
         */
    }
    private bool IsExcludedHighVolumeResult(
     int scanId,
     int scannerCweId)
    {
        /*
         * Global page switch.
         *
         * When disabled, nothing is considered excluded.
         */
        if (!ExcludeHighVolumeNoise)
        {
            return false;
        }

        /*
         * Count only Unrelated findings for this exact
         * ScanId + ScannerCweId combination.
         *
         * Do not use AggregatedSeries here because it combines
         * all relationship types into one count.
         */
        var unrelatedCount = RelatedSeries
            .Where(series =>
                series.ScanId == scanId)
            .SelectMany(series =>
                series.Items)
            .Where(item =>
                item.ScannerCweId == scannerCweId &&
                string.Equals(
                    item.Relationship,
                    nameof(CweRelationshipKind.Unrelated),
                    StringComparison.OrdinalIgnoreCase))
            .Sum(item =>
                item.Count);

        return unrelatedCount >
               HighFalsePositiveThreshold;
    }
    private bool IsExcludedHighVolumeUnrelatedResult(
    int scanId,
    int? scannerCweId,
    string? relationship)
    {
        if (!scannerCweId.HasValue)
        {
            return false;
        }

        if (!string.Equals(
                relationship,
                nameof(CweRelationshipKind.Unrelated),
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return IsExcludedHighVolumeResult(
            scanId,
            scannerCweId.Value);
    }



}
