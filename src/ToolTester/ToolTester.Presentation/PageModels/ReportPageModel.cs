using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Collections.ObjectModel;
using ToolTester.Application.CWETestResultBases.Queries;
using ToolTester.Application.Relationships.Queries;
using ToolTester.Application.Reports.Quiries;
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


        private bool _isNavigatedTo;
        private bool _dataLoaded;
        private readonly ModalErrorHandler _errorHandler;
        private readonly IMediator _mediator;

        public ObservableCollection<TestSeries> TestSeries
        {
            get => _testSeries;
            set => SetProperty(ref _testSeries, value); // SetProperty handles property change notification
        }
        public ObservableCollection<RelatedItemsInTest> ReletedItems
        {
            get => _relateditems;
            set => SetProperty(ref _relateditems, value); // SetProperty handles property change notification
        }
        public ObservableCollection<CweTestResults> Items
        {
            get => _items;
            set => SetProperty(ref _items, value); // SetProperty handles property change notification
        }
        public ReportPageModel(ModalErrorHandler errorHandler, IMediator mediator)
        {
            _errorHandler = errorHandler;
            _mediator = mediator;
        }

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
            var repquery = new GetReportsWithPaginationQuery()
            {
                PageSize = 10000
            };
            var represult = await _mediator.Send(repquery);
            ObservableCollection<RelatedItemsInTest> relatedItemsInTests = new ObservableCollection<RelatedItemsInTest>();
        
                foreach (var item in represult.Items)
            {
                var rep = new RelatedItemsInTest()
                {
                    Id = item.Id,
                    CweId = item.CweId,
                    RelatedId = item.RelatedId,
                    ScanId = item.ScanId,
                    ToolId = item.ToolId,
                    Count = item.Count,
                    Relationship = item.Relationship
                };
                relatedItemsInTests.Add(rep);
            }
            ReletedItems = new ObservableCollection<RelatedItemsInTest>(relatedItemsInTests);
           

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



            foreach(var group in result.Items.GroupBy(d=>d.ScanId))
            {
                var testrel = new TestSeries();
                testrel.ScanId = group.Key;
                foreach (var item in group)
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
                    if (testrel.Items == null) testrel.Items = new List<CweTestResults>();
                    testrel.Items.Add(rel);

                }
                testseries.Add(testrel);
            }         
            //Maui requires this to do initial load. 
            TestSeries = new ObservableCollection<TestSeries>(testseries);
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

    }
    public class TestSeries()
    {
        public int ScanId { get; set; }
        public List<CweTestResults> Items { get; set; }
    }

}
