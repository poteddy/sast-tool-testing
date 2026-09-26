using ClosedXML.Excel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Collections.ObjectModel;
using ToolTester.Application.CWETestResultBases.Queries;
using ToolTester.Application.Reports.Quiries;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.Services;

namespace ToolTester.Presentation.PageModels;

public partial class ReportPageModel : BaseViewModel
{
    private const int TestResultPageSize = 100000;
    private const int ReportPageSize = 10000;
    private const int RelatedErrorValue = 0;
    private const int UnrelatedErrorValue = 5;

    private readonly ModalErrorHandler _errorHandler;
    private readonly IMediator _mediator;

    private ObservableCollection<CweTestResults> _items = [];
    private ObservableCollection<TestSeries> _testSeries = [];
    private ObservableCollection<AggrigatedSeries> _aggregatedSeries = [];
    private ObservableCollection<RelatedSeries> _relatedSeries = [];

    private bool _isNavigatedTo;
    private bool _isLoading;
    private bool _dataLoaded;
    private string _selectedRelationship = "All";

    public ReportPageModel(
        ModalErrorHandler errorHandler,
        IMediator mediator)
    {
        _errorHandler = errorHandler;
        _mediator = mediator;
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

    public string SelectedRelationship
    {
        get => _selectedRelationship;
        set => SetProperty(ref _selectedRelationship, value);
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
         * This lookup makes the saved Reports table the authoritative
         * source for relationship scoring on the report page.
         *
         * Assumed mapping:
         *   Report.CweId     = scanner CWE
         *   Report.RelatedId = ground-truth CWE
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

                    Items = group
                        .OrderBy(report => report.GroundTruthCweId)
                        .ThenBy(report => report.ScannerCweId)
                        .Select(report => new RelatedItemsInTest
                        {
                            Id = report.Id,
                            GroundTruthCweId = report.GroundTruthCweId,
                            ScannerCweId = report.ScannerCweId,
                            ScanId = report.ScanId,
                            ToolId = report.ToolId,
                            Count = report.Count,
                            Relationship = report.Relationship,
                            RelationshipScore =
                                report.RelationshipScore
                        })
                        .ToList()
                }));

        OnPropertyChanged(nameof(AvailableRelationships));

        /*
         * Join every raw test result to its saved report.
         *
         * If no matching report exists, the item receives the unrelated
         * error value. This also makes missing report generation visible
         * in the chart instead of evaluating the relationship again.
         */
        var mappedItems = testResult.Items
            .Select(item =>
            {
                var reportKey = (
                    item.ScanId,
                    ScannerCweId: item.ScannerFoundCWE,
                    GroundTruthCweId: item.TestPathListedCWE);

                var hasSavedReport = reportLookup.TryGetValue(
                    reportKey,
                    out var savedReport);

                return new CweTestResults
                {
                    Id = item.Id,
                    ScannerFoundCWE = item.ScannerFoundCWE,
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

        /*
         * Reuse the already mapped Items instead of repeating the
         * report lookup and object mapping for TestSeries.
         */
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
         * AggregatedSeries still uses the raw scanner results because
         * it represents finding counts by scanner CWE.
         */
        AggregatedSeries =
            new ObservableCollection<AggrigatedSeries>(
                testResult.Items
                    .GroupBy(item => item.ScanId)
                    .OrderBy(group => group.Key)
                    .Select(group => new AggrigatedSeries
                    {
                        ScanId = group.Key,

                        Items = group
                            .GroupBy(item =>
                                item.ScannerFoundCWE)
                            .OrderBy(cweGroup =>
                                cweGroup.Key)
                            .Select(cweGroup =>
                                new AggrigatedItems
                                {
                                    CweId = cweGroup.Key,
                                    Count = cweGroup.Count()
                                })
                            .ToList()
                    }));

        ReportDataLoaded?.Invoke(
            this,
            EventArgs.Empty);
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
             * Allow the MAUI UI thread to render the loading indicator
             * before database and collection work begins.
             */
            await Task.Yield();

            await LoadItemsAsync();

            _dataLoaded = true;
            OnPropertyChanged(nameof(HasLoadedData));
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
            "Scan Id";

        worksheet.Cell(1, 4).Value =
            "Error Value";

        ApplyHeaderStyle(
            worksheet.Range(1, 1, 1, 4));

        var row = 2;

        foreach (var item in Items)
        {
            worksheet.Cell(row, 1).Value =
                item.TestPathListedCWE;

            worksheet.Cell(row, 2).Value =
                item.ScannerFoundCWE;

            worksheet.Cell(row, 3).Value =
                item.ScanId;

            worksheet.Cell(row, 4).Value =
                item.ErrorValue;

            row++;
        }

        CreateTableIfDataExists(
            worksheet,
            "ResultsTable",
            row - 1,
            4);

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
    }

    private void AddRelationshipsWorksheet(
        XLWorkbook workbook)
    {
        var worksheet = workbook.Worksheets.Add(
            "Relationships");

        worksheet.Cell(1, 1).Value =
            "Scan Id";

        worksheet.Cell(1, 2).Value =
            "CWE";

        worksheet.Cell(1, 3).Value =
            "Related CWE";

        worksheet.Cell(1, 4).Value =
            "Relationship";

        worksheet.Cell(1, 5).Value =
            "Score";

        worksheet.Cell(1, 6).Value =
            "Count";

        ApplyHeaderStyle(
            worksheet.Range(1, 1, 1, 6));

        var row = 2;

        foreach (var series in RelatedSeries)
        {
            foreach (var item in series.Items)
            {
                worksheet.Cell(row, 1).Value =
                    item.ScanId;

                worksheet.Cell(row, 2).Value =
                    item.GroundTruthCweId;

                worksheet.Cell(row, 3).Value =
                    item.ScannerCweId;

                worksheet.Cell(row, 4).Value =
                    item.Relationship;

                worksheet.Cell(row, 5).Value =
                    item.RelationshipScore;

                worksheet.Cell(row, 6).Value =
                    item.Count;

                row++;
            }
        }

        CreateTableIfDataExists(
            worksheet,
            "RelationshipsTable",
            row - 1,
            6);

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
    }

    private void AddAggregatedWorksheet(
        XLWorkbook workbook)
    {
        var worksheet = workbook.Worksheets.Add(
            "Aggregated");

        worksheet.Cell(1, 1).Value =
            "Scan Id";

        worksheet.Cell(1, 2).Value =
            "CWE";

        worksheet.Cell(1, 3).Value =
            "Count";

        ApplyHeaderStyle(
            worksheet.Range(1, 1, 1, 3));

        var row = 2;

        foreach (var series in AggregatedSeries)
        {
            foreach (var item in series.Items)
            {
                worksheet.Cell(row, 1).Value =
                    series.ScanId;

                worksheet.Cell(row, 2).Value =
                    item.CweId;

                worksheet.Cell(row, 3).Value =
                    item.Count;

                row++;
            }
        }

        CreateTableIfDataExists(
            worksheet,
            "AggregatedTable",
            row - 1,
            3);

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
            .Range(1, 1, lastRow, lastColumn)
            .CreateTable(tableName);
    }
}

public class TestSeries
{
    public int ScanId { get; set; }

    public List<CweTestResults> Items { get; set; } = [];
}

public class RelatedSeries
{
    public int ScanId { get; set; }

    public List<RelatedItemsInTest> Items { get; set; } = [];
}

public class AggrigatedSeries
{
    public int ScanId { get; set; }

    public List<AggrigatedItems> Items { get; set; } = [];
}

public class AggrigatedItems
{
    public int CweId { get; set; }

    public int Count { get; set; }
}