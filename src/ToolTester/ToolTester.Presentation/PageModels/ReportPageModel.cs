using ClosedXML.Excel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.ExtendedProperties;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using ToolTester.Application.CWETestResultBases.Queries;
using ToolTester.Application.Reports.Quiries;
using ToolTester.Infrastructure.Persistance;
using ToolTester.Infrastructure.Services;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.Services;

namespace ToolTester.Presentation.PageModels;

public partial class ReportPageModel : BaseViewModel
{
    private const int TestResultPageSize = 250000;
    private const int ReportPageSize = 10000;
    private const int RelatedErrorValue = 0;
    private const int UnrelatedErrorValue = 5;
    private const int ParetoCweLimit = 25;

    private readonly BenchmarkReportService _benchmarkService;
    private readonly ModalErrorHandler _errorHandler;
    private readonly IMediator _mediator;

    private readonly IDbContextFactory<ApplicationDbContext>
        _contextFactory;

    private IReadOnlyDictionary<int, string> _scanNames =
        new Dictionary<int, string>();

    private ObservableCollection<CweTestResults> _items = [];
    private ObservableCollection<TestSeries> _testSeries = [];
    private ObservableCollection<AggrigatedSeries> _aggregatedSeries = [];
    private ObservableCollection<RelatedSeries> _relatedSeries = [];
    private ObservableCollection<FalseNegativeParetoSeries>
        _falseNegativeParetoSeries = [];

    private ObservableCollection<RelationshipMixSeries>  _relationshipMixSeries = [];

    public ObservableCollection<RelationshipMixSeries>
    RelationshipMixSeries
    {
        get => _relationshipMixSeries;
        private set => SetProperty(
        ref _relationshipMixSeries,
        value);
    }

    private ObservableCollection<RelationshipMixChartPoint>
    _relationshipMixChartData = [];

    public ObservableCollection<RelationshipMixChartPoint>
        RelationshipMixChartData
    {
        get => _relationshipMixChartData;
        private set => SetProperty(
            ref _relationshipMixChartData,
            value);
    }
    private bool _isNavigatedTo;
    private bool _isLoading;
    private bool _dataLoaded;
    private string _selectedRelationship = "All";

    public ReportPageModel(
      ModalErrorHandler errorHandler,
      IMediator mediator,
      BenchmarkReportService benchmarkService,
      IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _errorHandler = errorHandler
            ?? throw new ArgumentNullException(
                nameof(errorHandler));

        _mediator = mediator
            ?? throw new ArgumentNullException(
                nameof(mediator));

        _benchmarkService = benchmarkService
            ?? throw new ArgumentNullException(
                nameof(benchmarkService));

        _contextFactory = contextFactory
            ?? throw new ArgumentNullException(
                nameof(contextFactory));
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

    /// <summary>
    /// Contains one Pareto data series for each scanner.
    /// Each scanner series contains its top missed CWEs.
    /// </summary>
    public ObservableCollection<FalseNegativeParetoSeries>
        FalseNegativeParetoSeries
    {
        get => _falseNegativeParetoSeries;
        private set => SetProperty(
            ref _falseNegativeParetoSeries,
            value);
    }

    public string SelectedRelationship
    {
        get => _selectedRelationship;
        set => SetProperty(
            ref _selectedRelationship,
            value);
    }

    public List<string> AvailableRelationships =>
    [
        "All",
        .. RelatedSeries
            .SelectMany(series => series.Items)
            .Select(item => item.Relationship)
            .Where(relationship =>
                !string.IsNullOrWhiteSpace(relationship))
            .Distinct(StringComparer.OrdinalIgnoreCase)
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

        /*
         * GenerateReportAsync has already evaluated each distinct pair
         * and saved its relationship and score.
         *
         * The saved Reports table is the authoritative source for
         * relationship scoring on this page.
         *
         * Mapping:
         *   Report.ScannerCweId = scanner-reported CWE
         *   Report.GroundTruthCweId = Juliet ground-truth CWE
         */
        var reportLookup = reportResult.Items
            .GroupBy(report => (
                report.ScanId,
                ScannerCweId: report.ScannerCweId,
                GroundTruthCweId: report.GroundTruthCweId))
            .ToDictionary(
                group => group.Key,
                group => group.First());

        RelatedSeries = new ObservableCollection<RelatedSeries>(
            reportResult.Items
                .GroupBy(report => report.ScanId)
                .OrderBy(group => group.Key)
                .Select(group => new RelatedSeries
                {
                    ScanId = group.Key,
                    ScanName = GetScanName(group.Key),
                    Items = group
                        .OrderBy(report =>
                            report.GroundTruthCweId)
                        .ThenBy(report =>
                            report.ScannerCweId)
                        .Select(report =>
                            new RelatedItemsInTest
                            {
                                Id = report.Id,
                                GroundTruthCweId =
                                    report.GroundTruthCweId,
                                ScannerCweId =
                                    report.ScannerCweId,
                                ScanId = report.ScanId,
                                ToolId = report.ToolId,
                                Count = report.Count,
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
        /*
         * Join every raw test result to its saved relationship report.
         *
         * A result is treated as related when it has a saved report
         * with a relationship score greater than zero.
         *
         * A missing saved report is treated as unrelated so that gaps
         * in report generation remain visible.
         */
        var mappedItems = testResult.Items
    .Select(item =>
    {
        var reportKey = (
            item.ScanId,
            ScannerCweId: item.ScannerFoundCWE,
            GroundTruthCweId:
                item.TestPathListedCWE);

        var hasSavedReport =
            reportLookup.TryGetValue(
                reportKey,
                out var savedReport);

        return new CweTestResults
        {
            Id = item.Id,
            ScannerFoundCWE =
                item.ScannerFoundCWE,
            ScanId = item.ScanId,
            TestPathListedCWE =
                item.TestPathListedCWE,

            ErrorValue =
                hasSavedReport &&
                savedReport!.RelationshipScore > 0
                    ? RelatedErrorValue
                    : UnrelatedErrorValue
        };
    })
    .ToList();

        Items = new ObservableCollection<CweTestResults>(
            mappedItems);

        TestSeries = new ObservableCollection<TestSeries>(
            mappedItems
                .GroupBy(item => item.ScanId)
                .OrderBy(group => group.Key)
                .Select(group => new TestSeries
                {
                    ScanId = group.Key,
                    Items = group.ToList()
                }));

        /*
         * AggregatedSeries represents scanner findings grouped by
         * scanner-reported CWE.
         */
        AggregatedSeries =
            new ObservableCollection<AggrigatedSeries>(
                testResult.Items
                    .GroupBy(item => item.ScanId)
                    .OrderBy(group => group.Key)
                    .Select(group =>
                        new AggrigatedSeries
                        {
                            ScanId = group.Key,
                            ScanName = GetScanName(group.Key),
                            Items = group
                                .GroupBy(item =>
                                    item.ScannerFoundCWE)
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

    /// <summary>
    /// Builds a separate top-25 false-negative Pareto series for each scanner.
    /// The cumulative percentage restarts at zero for every scanner.
    /// </summary>
    private void BuildFalseNegativePareto(
    IEnumerable<ScannerFalseNegativeDto> data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var source = data
            .Where(item =>
                item.FalseNegatives > 0)
            .ToList();

        var scannerSeries = source
            .GroupBy(item =>
                item.ScanId)
            .OrderBy(group =>
                group.Key)
            .Select(scannerGroup =>
            {
                var scanId =
                    scannerGroup.Key;

                var scanName =
                    GetScanName(scanId);

                var scanDisplayName =
                    GetScanDisplayName(scanId);

                var topMisses = scannerGroup
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
                    .Take(ParetoCweLimit)
                    .ToList();

                var totalFalseNegatives =
                    topMisses.Sum(item =>
                        item.FalseNegatives);

                var runningFalseNegatives = 0;

                var points = topMisses
                    .Select(item =>
                    {
                        runningFalseNegatives +=
                            item.FalseNegatives;

                        var cumulativePercent =
                            totalFalseNegatives == 0
                                ? 0
                                : runningFalseNegatives *
                                  100.0 /
                                  totalFalseNegatives;

                        return new FalseNegativeChartPoint
                        {
                            ScanId =
                                scanId,

                            ScannerName =
                                scanDisplayName,

                            CweId =
                                item.CweId,

                            CweName =
                                item.CweName,

                            CweLabel =
                                $"CWE-{item.CweId}",

                            Opportunities =
                                item.Opportunities,

                            Detected =
                                item.Detected,

                            FalseNegatives =
                                item.FalseNegatives,

                            CumulativePercent =
                                cumulativePercent
                        };
                    })
                    .ToList();

                return new FalseNegativeParetoSeries
                {
                    ScanId =
                        scanId,

                    ScanName =
                        scanName,

                    ScannerName =
                        scanDisplayName,

                    Items =
                        points
                };
            })
            .ToList();

        FalseNegativeParetoSeries =
            new ObservableCollection<
                FalseNegativeParetoSeries>(
                scannerSeries);
    }
    private static readonly string[]
  RelationshipDisplayOrder =
  [
  "Exact",
"DirectSibling",
"SameRootCauseBroaderCwe",
"DirectParent",
"DirectChild",
"SharedAncestor",
"CanPrecede",
"Unrelated"
  ];

    private void BuildRelationshipMix()
    {
        var chartPoints =
            new List<RelationshipMixChartPoint>();

        var mixSeries =
            new List<RelationshipMixSeries>();

        foreach (var relatedSeries in
                 RelatedSeries.OrderBy(series =>
                     series.ScanId))
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
                        item =>
                            item.Relationship,
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        group =>
                            group.Key,
                        group =>
                            group.Sum(item =>
                                item.Count),
                        StringComparer.OrdinalIgnoreCase);

            var totalCount =
                relationshipCounts.Values.Sum();

            if (totalCount <= 0)
            {
                continue;
            }

            var runningPercent = 0.0;

            var seriesPoints =
                new List<RelationshipMixPoint>();

            foreach (var relationship in
                     RelationshipDisplayOrder)
            {
                relationshipCounts.TryGetValue(
                    relationship,
                    out var count);

                var percentage =
                    count * 100.0 /
                    totalCount;

                var low =
                    runningPercent;

                var high =
                    runningPercent +
                    percentage;

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
            new ObservableCollection<
                RelationshipMixSeries>(
                mixSeries);

        RelationshipMixChartData =
            new ObservableCollection<
                RelationshipMixChartPoint>(
                chartPoints);
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

    private async Task ExecuteLoadAsync()
    {
        IsLoading = true;

        try
        {
            /*
             * Give the MAUI UI thread an opportunity to display
             * the activity indicator before loading begins.
             */
            await Task.Yield();

            await LoadItemsAsync();

            var falseNegatives =
                await _benchmarkService
                    .GetFalseNegativesByScannerAsync();

            BuildFalseNegativePareto(
                falseNegatives);

            _dataLoaded = true;

            OnPropertyChanged(
                nameof(HasLoadedData));

            /*
             * Raise this only after all report collections,
             * including the Pareto data, have been populated.
             */
            ReportDataLoaded?.Invoke(
                this,
                EventArgs.Empty);
        }
        catch (Exception exception)
        {
            _errorHandler.HandleError(
                exception);
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
            using var workbook =
                new XLWorkbook();

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
            _errorHandler.HandleError(
                exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddResultsWorksheet(
    XLWorkbook workbook)
    {
        var worksheet = workbook.Worksheets.Add(
            "Results");

        worksheet.Cell(1, 1).Value =
            "Ground Truth CWE";

        worksheet.Cell(1, 2).Value =
            "Scanner CWE";

        worksheet.Cell(1, 3).Value =
            "Scan Name";

        worksheet.Cell(1, 4).Value =
            "Scan Id";

        worksheet.Cell(1, 5).Value =
            "Error Value";

        ApplyHeaderStyle(
            worksheet.Range(1, 1, 1, 5));

        var row = 2;

        foreach (var item in Items)
        {
            worksheet.Cell(row, 1).Value =
                item.TestPathListedCWE;

            worksheet.Cell(row, 2).Value =
                item.ScannerFoundCWE;

            worksheet.Cell(row, 3).Value =
                GetScanName(item.ScanId);

            worksheet.Cell(row, 4).Value =
                item.ScanId;

            worksheet.Cell(row, 5).Value =
                item.ErrorValue;

            row++;
        }

        CreateTableIfDataExists(
            worksheet,
            "ResultsTable",
            row - 1,
            5);

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
    }

    private void AddRelationshipsWorksheet(
      XLWorkbook workbook)
    {
        var worksheet = workbook.Worksheets.Add(
            "Relationships");

        worksheet.Cell(1, 1).Value =
            "Scan Name";

        worksheet.Cell(1, 2).Value =
            "Scan Id";

        worksheet.Cell(1, 3).Value =
            "Ground Truth CWE";

        worksheet.Cell(1, 4).Value =
            "Scanner CWE";

        worksheet.Cell(1, 5).Value =
            "Relationship";

        worksheet.Cell(1, 6).Value =
            "Score";

        worksheet.Cell(1, 7).Value =
            "Count";

        ApplyHeaderStyle(
            worksheet.Range(1, 1, 1, 7));

        var row = 2;

        foreach (var series in RelatedSeries)
        {
            foreach (var item in series.Items)
            {
                worksheet.Cell(row, 1).Value =
                    series.ScanName;

                worksheet.Cell(row, 2).Value =
                    series.ScanId;

                worksheet.Cell(row, 3).Value =
                    item.GroundTruthCweId;

                worksheet.Cell(row, 4).Value =
                    item.ScannerCweId;

                worksheet.Cell(row, 5).Value =
                    item.Relationship;

                worksheet.Cell(row, 6).Value =
                    item.RelationshipScore;

                worksheet.Cell(row, 7).Value =
                    item.Count;

                row++;
            }
        }

        CreateTableIfDataExists(
            worksheet,
            "RelationshipsTable",
            row - 1,
            7);

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
    }

    private void AddAggregatedWorksheet(
     XLWorkbook workbook)
    {
        var worksheet = workbook.Worksheets.Add(
            "Aggregated");

        worksheet.Cell(1, 1).Value =
            "Scan Name";

        worksheet.Cell(1, 2).Value =
            "Scan Id";

        worksheet.Cell(1, 3).Value =
            "CWE";

        worksheet.Cell(1, 4).Value =
            "Count";

        ApplyHeaderStyle(
            worksheet.Range(1, 1, 1, 4));

        var row = 2;

        foreach (var series in AggregatedSeries)
        {
            foreach (var item in series.Items)
            {
                worksheet.Cell(row, 1).Value =
                    series.ScanName;

                worksheet.Cell(row, 2).Value =
                    series.ScanId;

                worksheet.Cell(row, 3).Value =
                    item.CweId;

                worksheet.Cell(row, 4).Value =
                    item.Count;

                row++;
            }
        }

        CreateTableIfDataExists(
            worksheet,
            "AggregatedTable",
            row - 1,
            4);

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
    }

    private void AddFalseNegativesWorksheet(
      XLWorkbook workbook)
    {
        var worksheet = workbook.Worksheets.Add(
            "False Negatives");

        worksheet.Cell(1, 1).Value =
            "Scan Name";

        worksheet.Cell(1, 2).Value =
            "Scan Id";

        worksheet.Cell(1, 3).Value =
            "CWE";

        worksheet.Cell(1, 4).Value =
            "CWE Name";

        worksheet.Cell(1, 5).Value =
            "Known Opportunities";

        worksheet.Cell(1, 6).Value =
            "Detected";

        worksheet.Cell(1, 7).Value =
            "False Negatives";

        worksheet.Cell(1, 8).Value =
            "Cumulative Percent";

        ApplyHeaderStyle(
            worksheet.Range(1, 1, 1, 8));

        var row = 2;

        foreach (var series in
                 FalseNegativeParetoSeries)
        {
            foreach (var item in series.Items)
            {
                worksheet.Cell(row, 1).Value =
                    series.ScanName;

                worksheet.Cell(row, 2).Value =
                    series.ScanId;

                worksheet.Cell(row, 3).Value =
                    item.CweLabel;

                worksheet.Cell(row, 4).Value =
                    item.CweName;

                worksheet.Cell(row, 5).Value =
                    item.Opportunities;

                worksheet.Cell(row, 6).Value =
                    item.Detected;

                worksheet.Cell(row, 7).Value =
                    item.FalseNegatives;

                worksheet.Cell(row, 8).Value =
                    item.CumulativePercent / 100.0;

                row++;
            }
        }

        if (row > 2)
        {
            worksheet
                .Range(2, 8, row - 1, 8)
                .Style
                .NumberFormat
                .Format = "0.00%";
        }

        CreateTableIfDataExists(
            worksheet,
            "FalseNegativesTable",
            row - 1,
            8);

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
    }

    private void AddSummaryWorksheet(
        XLWorkbook workbook)
    {
        var worksheet = workbook.Worksheets.Add(
            "Summary");

        worksheet.Cell(1, 1).Value =
            "Metric";

        worksheet.Cell(1, 2).Value =
            "Value";

        ApplyHeaderStyle(
            worksheet.Range(1, 1, 1, 2));

        worksheet.Cell(2, 1).Value =
            "Total Results";

        worksheet.Cell(2, 2).Value =
            Items.Count;

        worksheet.Cell(3, 1).Value =
            "Scan Series";

        worksheet.Cell(3, 2).Value =
            TestSeries.Count;

        worksheet.Cell(4, 1).Value =
            "Relationship Series";

        worksheet.Cell(4, 2).Value =
            RelatedSeries.Count;

        worksheet.Cell(5, 1).Value =
            "Aggregated Series";

        worksheet.Cell(5, 2).Value =
            AggregatedSeries.Count;

        worksheet.Cell(6, 1).Value =
            "Saved Relationship Rows";

        worksheet.Cell(6, 2).Value =
            RelatedSeries.Sum(series =>
                series.Items.Count);

        worksheet.Cell(7, 1).Value =
            "Scanners With False Negatives";

        worksheet.Cell(7, 2).Value =
            FalseNegativeParetoSeries.Count;

        worksheet.Cell(8, 1).Value =
            "Displayed False Negatives";

        worksheet.Cell(8, 2).Value =
            FalseNegativeParetoSeries
                .SelectMany(series =>
                    series.Items)
                .Sum(item =>
                    item.FalseNegatives);

        worksheet.Columns().AdjustToContents();
    }

    private static void ApplyHeaderStyle(
        IXLRange headerRange)
    {
        headerRange.Style.Font.Bold = true;

        headerRange.Style.Fill.BackgroundColor =
            XLColor.LightGray;
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

        worksheet
            .Range(
                1,
                1,
                lastRow,
                lastColumn)
            .CreateTable(tableName);
    }
    private async Task LoadScanNamesAsync(
    CancellationToken cancellationToken)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        _scanNames = await context.Scans
            .AsNoTracking()
           
            .ToDictionaryAsync(
                scan => scan.Id,
                scan => string.IsNullOrWhiteSpace(scan.Name)
                    ? $"Scan {scan.Id}"
                    : scan.Name,
                cancellationToken);
    }

    private string GetScanName(
        int scanId)
    {
        return _scanNames.TryGetValue(
            scanId,
            out var scanName)
                ? scanName
                : $"Scan {scanId}";
    }

    private string GetScanDisplayName(
        int scanId)
    {
        return $"{GetScanName(scanId)} ({scanId})";
    }
}

public sealed class FalseNegativeParetoSeries
{
    public int ScanId { get; set; }

    public string ScanName { get; set; } =
        string.Empty;

    public string ScannerName { get; set; } =
        string.Empty;

    public List<FalseNegativeChartPoint> Items { get; set; } =
        [];
}

public sealed class FalseNegativeChartPoint
{
    public int ScanId { get; set; }

    public string ScannerName { get; set; } =
        string.Empty;

    public int CweId { get; set; }

    public string CweName { get; set; } =
        string.Empty;

    public string CweLabel { get; set; } =
        string.Empty;

    public int Opportunities { get; set; }

    public int Detected { get; set; }

    public int FalseNegatives { get; set; }

    public double CumulativePercent { get; set; }
}

public class TestSeries
{
    public int ScanId { get; set; }

    public string ScanName { get; set; } =
        string.Empty;

    public List<CweTestResults> Items { get; set; } =
        [];
}

public class RelatedSeries
{
    public int ScanId { get; set; }

    public string ScanName { get; set; } =
        string.Empty;

    public List<RelatedItemsInTest> Items { get; set; } =
        [];
}

public class AggrigatedSeries
{
    public int ScanId { get; set; }

    public string ScanName { get; set; } =
        string.Empty;

    public List<AggrigatedItems> Items { get; set; } =
        [];
}

public class AggrigatedItems
{
    public int CweId { get; set; }

    public int Count { get; set; }
}

public sealed class RelationshipMixSeries
{
    public int ScanId { get; set; }

    public string ScanName { get; set; } =
        string.Empty;

    public List<RelationshipMixPoint> Items { get; set; } =
        [];
}

public sealed class RelationshipMixPoint
{
    public string Relationship { get; set; } =
        string.Empty;

    public int Count { get; set; }

    public double Percentage { get; set; }
}

public sealed class RelationshipMixChartPoint
{
    public int ScanId { get; set; }

    public string ScanName { get; set; } =
        string.Empty;

    /*
     * Keep this property if the existing XAML charts bind to
     * ScannerName. Its value is now derived from Scan.Name and Id.
     */
    public string ScannerName { get; set; } =
        string.Empty;

    public string Relationship { get; set; } =
        string.Empty;

    public int Count { get; set; }

    public double Percentage { get; set; }

    public double Low { get; set; }

    public double High { get; set; }
}