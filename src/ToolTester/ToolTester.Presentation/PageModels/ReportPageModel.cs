using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Collections.ObjectModel;
using ToolTester.Application.CWETestResultBases.Queries;
using ToolTester.Application.Relationships.Queries;
using ToolTester.Application.Reports.Quiries;
using ToolTester.Infrastructure.Services;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.Services;
using ToolTester.Presentation.Ulitlities;

namespace ToolTester.Presentation.PageModels
{
    public partial class ReportPageModel : BaseViewModel
    {
        private ObservableCollection<CweTestResults> _items = [];
        private ObservableCollection<TestSeries> _testSeries = [];
        private ObservableCollection<RelatedItemsInTest> _relateditems = [];
        private ObservableCollection<AggrigatedSeries> _aggrigatedseries = [];
        private ObservableCollection<RelatedSeries> _relatedseries = [];


        private bool _isNavigatedTo;
        private bool _dataLoaded;
        private readonly ModalErrorHandler _errorHandler;
        private readonly IMediator _mediator;

        public ObservableCollection<TestSeries> TestSeries
        {
            get => _testSeries;
            set => SetProperty(ref _testSeries, value); // SetProperty handles property change notification
        }
        public ObservableCollection<RelatedSeries> RelatedSeries
        {
            get => _relatedseries;
            set => SetProperty(ref _relatedseries, value); // SetProperty handles property change notification
        }
        public ObservableCollection<AggrigatedSeries> AggregatedSeries
        {
            get => _aggrigatedseries;
            set => SetProperty(ref _aggrigatedseries, value); // SetProperty handles property change notification
        }
        public ObservableCollection<CweTestResults> Items
        {
            get => _items;
            set => SetProperty(ref _items, value); // SetProperty handles property change notification
        }
        public ReportPageModel(ModalErrorHandler errorHandler, IMediator mediator, ICweRelationshipService relationshipService)
        {
            _errorHandler = errorHandler;
            _mediator = mediator;
            _relationshipService = relationshipService;
        }
        private ICweRelationshipService _relationshipService;

        public async Task LoadItemsAsync()
        {
            var query = new GetCweTestResultBasesWithPaginationQuery()
            {
                PageSize = 100000
            };
            var result = await _mediator.Send(query);

            var relquery = new GetRelationshipsWithPaginationQuery()
            {
                PageSize = 10000
            };
            var relresul = await _mediator.Send(relquery);
            ObservableCollection<CweTestResults> items = new ObservableCollection<CweTestResults>();
            ObservableCollection<TestSeries> testseries = new ObservableCollection<TestSeries>();
            ObservableCollection<AggrigatedSeries> aggrigatedSeries = new ObservableCollection<AggrigatedSeries>();
            var repquery = new GetReportsWithPaginationQuery()
            {
                PageSize = 10000
            };
            var represult = await _mediator.Send(repquery);
            ObservableCollection<RelatedSeries> relatedSeries = new ObservableCollection<RelatedSeries>();

            foreach (var group in represult.Items.GroupBy(d => d.ScanId))
            {
                var testrel = new RelatedSeries();
                testrel.Items ??= new List<RelatedItemsInTest>();

                testrel.Items.AddRange(
                    group
                       // .Where(x => x.Relationship != CweRelationshipKind.Unrelated.ToString())
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
                             
                        }));

                relatedSeries.Add(testrel);
            }
            RelatedSeries = new ObservableCollection<RelatedSeries>(relatedSeries);


            foreach (var item in result.Items)
            {
                var rel = new CweTestResults()
                {
                    Id = item.Id,
                    ScannerFoundCWE = item.ScannerFoundCWE,
                    ScanId = item.ScanId,
                    TestPathListedCWE = item.TestPathListedCWE,
                };

                if (!relresul.Items.Any(d => d.RelatedCweID == rel.ScannerFoundCWE && d.CWEID == rel.TestPathListedCWE))
                {
                    rel.ErrorValue = 5;
                }

                items.Add(rel);

            }
            //Maui requires this to do initial load. 
            Items = new ObservableCollection<CweTestResults>(items);



            foreach (var group in result.Items.GroupBy(x => x.ScanId))
            {
                var testSeries = new TestSeries
                {
                    ScanId = group.Key,
                    Items = new()
                };

                foreach (var item in group)
                {
                    var relationship = await _relationshipService.EvaluateAsync(
                        scannerCweId: item.ScannerFoundCWE,
                        groundTruthCweId: item.TestPathListedCWE,
                        "",
                        "",
                        CancellationToken.None
                        );

                    testSeries.Items.Add(new CweTestResults
                    {
                        Id = item.Id,
                        ScanId = item.ScanId,
                        ScannerFoundCWE = item.ScannerFoundCWE,
                        TestPathListedCWE = item.TestPathListedCWE,
                         
                        //Relationship = relationship.Relationship.ToString(),
                        //Score = relationship.Score,

                        ErrorValue = relationship.Score > 0
                            ? 0
                            : 5
                    });
                }

                testseries.Add(testSeries);
            }

            //Maui requires this to do initial load. 
            TestSeries = new ObservableCollection<TestSeries>(testseries);


            foreach (var group in result.Items.GroupBy(d => d.ScanId))
            {
                var testrel = new AggrigatedSeries()
                {
                    ScanId = group.Key,
                    Items = group
           .GroupBy(d => d.ScannerFoundCWE)
           .Select(g => new AggrigatedItems
           {
               CweId = g.Key,
               Count = g.Count(), // Aggregating the count

           }).ToList(),


                };
                aggrigatedSeries.Add(testrel);

            }
            //Maui requires this to do initial load. 
            AggregatedSeries = new ObservableCollection<AggrigatedSeries>(aggrigatedSeries);
        }

        [RelayCommand]
        private void NavigatedTo() =>
        _isNavigatedTo = true;

        [RelayCommand]
        private void NavigatedFrom() =>
            _isNavigatedTo = false;


        [RelayCommand]
        private async Task Appearing()
        {
            if (!_dataLoaded)
            {

                //await InitData(_seedDataService);
                _dataLoaded = true;
                await Refresh();
            }
            // This means we are being navigated to
            else if (!_isNavigatedTo)
            {
                await Refresh();
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            try
            {
                IsRefreshing = true;
                LoadItemsAsync().FireAndForgetSafeAsync(_errorHandler);
            }
            catch (Exception e)
            {

            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private Task AddTask()
              => Shell.Current.GoToAsync($"task");

        [RelayCommand]
        private Task NavigateToProject(CweCatalogDetailsPage project)
            => Shell.Current.GoToAsync($"project?id={project.ID}");

        public string SelectedRelationship { get; set; } = "All";

        public List<string> AvailableRelationships =>
        [
            "All",
    .. _relatedseries
        .SelectMany(x => x.Items)
        .Select(x => x.Relationship)
        .Distinct()
        .OrderBy(x => x)
        ];
    }
}
    public class TestSeries()
    {
        public int ScanId { get; set; }
        public List<CweTestResults> Items { get; set; }
    }
    public class RelatedSeries()
    {
        public int ScanId { get; set; }
        public List<RelatedItemsInTest> Items { get; set; }
    }
    public class AggrigatedSeries()
    {
        public int ScanId { get; set; }
        public List<AggrigatedItems> Items { get; set; }
    }
    public class AggrigatedItems()
    {
        public int CweId { get; set; }
        public int Count { get; set; }
    }
