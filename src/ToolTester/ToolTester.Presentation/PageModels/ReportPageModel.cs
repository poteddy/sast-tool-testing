using ClosedXML.Excel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Collections.ObjectModel;
using ToolTester.Application.CWETestResultBases.Queries;
using ToolTester.Application.Relationships.Queries;
using ToolTester.Application.Reports.Quiries;
using ToolTester.Infrastructure.Services;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.Services;

namespace ToolTester.Presentation.PageModels;

public partial class ReportPageModel : BaseViewModel
{
    private readonly ModalErrorHandler _errorHandler;
    private readonly IMediator _mediator;
    private readonly ICweRelationshipService _relationshipService;

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
        IMediator mediator,
        ICweRelationshipService relationshipService)
    {
        _errorHandler = errorHandler;
        _mediator = mediator;
        _relationshipService = relationshipService;
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
            .Where(series => series.Items is not null)
            .SelectMany(series => series.Items)
            .Select(item => item.Relationship)
            .Where(relationship => !string.IsNullOrWhiteSpace(relationship))
            .Distinct()
            .OrderBy(relationship => relationship)
    ];

    public async Task InitializeAsync()
    {
        if (_dataLoaded || IsLoading)
            return;

        IsLoading = true;

        try
        {
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

    public async Task LoadItemsAsync()
    {
        var result = await _mediator.Send(
            new GetCweTestResultBasesWithPaginationQuery
            {
                PageSize = 100000
            });

        var relationshipResult = await _mediator.Send(
            new GetRelationshipsWithPaginationQuery
            {
                PageSize = 10000
            });

        var reportResult = await _mediator.Send(
            new GetReportsWithPaginationQuery
            {
                PageSize = 10000
            });

        RelatedSeries = new ObservableCollection<RelatedSeries>(
            reportResult.Items
                .GroupBy(item => item.ScanId)
                .Select(group => new RelatedSeries
                {
                    ScanId = group.Key,
                    Items = group
                        .Select(item => new RelatedItemsInTest
                        {
                            Id = item.Id,
                            CweId = item.CweId,
                            RelatedId = item.RelatedId,
                            ScanId = item.ScanId,
                            ToolId = item.ToolId,
                            Count = item.Count,
                            Relationship = item.Relationship,
                            RelationshipScore = item.RelationshipScore
                        })
                        .ToList()
                }));

        OnPropertyChanged(nameof(AvailableRelationships));

        var relatedLookup = relationshipResult.Items
            .Select(item => (item.RelatedCweID, item.CWEID))
            .ToHashSet();

        Items = new ObservableCollection<CweTestResults>(
            result.Items.Select(item => new CweTestResults
            {
                Id = item.Id,
                ScannerFoundCWE = item.ScannerFoundCWE,
                ScanId = item.ScanId,
                TestPathListedCWE = item.TestPathListedCWE,
                ErrorValue = relatedLookup.Contains(
                    (item.ScannerFoundCWE, item.TestPathListedCWE))
                    ? 0
                    : 5
            }));

        var relationshipCache =
            new Dictionary<(int ScannerCwe, int GroundTruthCwe), int>();

        var testSeries = new List<TestSeries>();

        foreach (var group in result.Items.GroupBy(item => item.ScanId))
        {
            var series = new TestSeries
            {
                ScanId = group.Key,
                Items = []
            };

            foreach (var item in group)
            {
                var key = (item.ScannerFoundCWE, item.TestPathListedCWE);

                if (!relationshipCache.TryGetValue(key, out var score))
                {
                    var relationship = await _relationshipService.EvaluateAsync(
                        scannerCweId: item.ScannerFoundCWE,
                        groundTruthCweId: item.TestPathListedCWE,
                        "",
                        "",
                        CancellationToken.None);

                    score = relationship.Score;
                    relationshipCache[key] = score;
                }

                series.Items.Add(new CweTestResults
                {
                    Id = item.Id,
                    ScanId = item.ScanId,
                    ScannerFoundCWE = item.ScannerFoundCWE,
                    TestPathListedCWE = item.TestPathListedCWE,
                    ErrorValue = score > 0 ? 0 : 5
                });
            }

            testSeries.Add(series);
        }

        TestSeries = new ObservableCollection<TestSeries>(testSeries);

        AggregatedSeries = new ObservableCollection<AggrigatedSeries>(
            result.Items
                .GroupBy(item => item.ScanId)
                .Select(group => new AggrigatedSeries
                {
                    ScanId = group.Key,
                    Items = group
                        .GroupBy(item => item.ScannerFoundCWE)
                        .Select(cweGroup => new AggrigatedItems
                        {
                            CweId = cweGroup.Key,
                            Count = cweGroup.Count()
                        })
                        .ToList()
                }));

        ReportDataLoaded?.Invoke(this, EventArgs.Empty);
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
            return;

        IsLoading = true;

        try
        {
            // Give MAUI a chance to paint the spinner
            
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
        try
        {
            IsLoading = true;

            using var workbook = new XLWorkbook();

            //
            // Results Sheet
            //
            var resultsSheet = workbook.Worksheets.Add("Results");

            resultsSheet.Cell(1, 1).Value = "Ground Truth CWE";
            resultsSheet.Cell(1, 2).Value = "Scanner CWE";
            resultsSheet.Cell(1, 3).Value = "Scan Id";
            resultsSheet.Cell(1, 4).Value = "Error Value";

            var row = 2;

            foreach (var item in Items)
            {
                resultsSheet.Cell(row, 1).Value = item.TestPathListedCWE;
                resultsSheet.Cell(row, 2).Value = item.ScannerFoundCWE;
                resultsSheet.Cell(row, 3).Value = item.ScanId;
                resultsSheet.Cell(row, 4).Value = item.ErrorValue;

                row++;
            }

            resultsSheet.Columns().AdjustToContents();

            //
            // Relationships Sheet
            //
            var relationshipSheet =
                workbook.Worksheets.Add("Relationships");

            relationshipSheet.Cell(1, 1).Value = "Scan Id";
            relationshipSheet.Cell(1, 2).Value = "CWE";
            relationshipSheet.Cell(1, 3).Value = "Related CWE";
            relationshipSheet.Cell(1, 4).Value = "Relationship";
            relationshipSheet.Cell(1, 5).Value = "Score";
            relationshipSheet.Cell(1, 6).Value = "Count";

            row = 2;

            foreach (var series in RelatedSeries)
            {
                foreach (var item in series.Items)
                {
                    relationshipSheet.Cell(row, 1).Value = item.ScanId;
                    relationshipSheet.Cell(row, 2).Value = item.CweId;
                    relationshipSheet.Cell(row, 3).Value = item.RelatedId;
                    relationshipSheet.Cell(row, 4).Value = item.Relationship;
                    relationshipSheet.Cell(row, 5).Value = item.RelationshipScore;
                    relationshipSheet.Cell(row, 6).Value = item.Count;

                    row++;
                }
            }

            relationshipSheet.Columns().AdjustToContents();

            //
            // Aggregated Sheet
            //
            var aggregatedSheet =
                workbook.Worksheets.Add("Aggregated");

            aggregatedSheet.Cell(1, 1).Value = "Scan Id";
            aggregatedSheet.Cell(1, 2).Value = "CWE";
            aggregatedSheet.Cell(1, 3).Value = "Count";

            row = 2;

            foreach (var series in AggregatedSeries)
            {
                foreach (var item in series.Items)
                {
                    aggregatedSheet.Cell(row, 1).Value = series.ScanId;
                    aggregatedSheet.Cell(row, 2).Value = item.CweId;
                    aggregatedSheet.Cell(row, 3).Value = item.Count;

                    row++;
                }
            }

            aggregatedSheet.Columns().AdjustToContents();

            //
            // Summary Sheet
            //
            var summarySheet =
                workbook.Worksheets.Add("Summary");

            summarySheet.Cell(1, 1).Value = "Metric";
            summarySheet.Cell(1, 2).Value = "Value";

            summarySheet.Cell(2, 1).Value = "Total Results";
            summarySheet.Cell(2, 2).Value = Items.Count;

            summarySheet.Cell(3, 1).Value = "Scan Series";
            summarySheet.Cell(3, 2).Value = TestSeries.Count;

            summarySheet.Cell(4, 1).Value = "Relationship Series";
            summarySheet.Cell(4, 2).Value = RelatedSeries.Count;

            summarySheet.Cell(5, 1).Value = "Aggregated Series";
            summarySheet.Cell(5, 2).Value = AggregatedSeries.Count;

            summarySheet.Columns().AdjustToContents();

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
        catch (Exception ex)
        {
            _errorHandler.HandleError(ex);
        }
        finally
        {
            IsLoading = false;
        }
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
