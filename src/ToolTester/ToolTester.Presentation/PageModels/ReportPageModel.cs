using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Collections.ObjectModel;
using ToolTester.Application.CWETestResultBases.Queries;
using ToolTester.Application.Relationships.Queries;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.Services;
using ToolTester.Presentation.Ulitlities;

namespace ToolTester.Presentation.PageModels
{
    public partial class ReportPageModel : BaseViewModel
    {
        private ObservableCollection<CweTestResults> _items = [];


        private bool _isNavigatedTo;
        private bool _dataLoaded;
        private readonly ModalErrorHandler _errorHandler;
        private readonly IMediator _mediator;



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




            foreach (var item in result.Items)
            {
                var rel = new CweTestResults()
                {
                    Id = item.Id,
                    ScannerFoundCWE = item.ScannerFoundCWE,
                    Test = item.Test,
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

            //SfCartesianChart chart = new SfCartesianChart();
            //NumericalAxis primaryAxis = new NumericalAxis();
            //chart.XAxes.Add(primaryAxis);
            //NumericalAxis secondaryAxis = new NumericalAxis();
            //chart.YAxes.Add(secondaryAxis);

            //// Create a scatter series to plot data points
            //ScatterSeries scatterSeries = new ScatterSeries()
            //{
            //    ItemsSource = new ViewModel().EnergyProductions,
            //    XBindingPath = "ID",
            //    YBindingPath = "Coal",
            //    PointWidth = 20,
            //    PointHeight = 20
            //};

            //// Create an error bar series to display error ranges
            //ErrorBarSeries errorBar = new ErrorBarSeries()
            //{
            //    ItemsSource = new ViewModel().EnergyProductions,
            //    XBindingPath = "ID",
            //    YBindingPath = "Coal",
            //    HorizontalErrorValue = 0.5,
            //    VerticalErrorValue = 50
            //};

            //// Add the both series to the chart's series collection
            //chart.Series.Add(scatterSeries);
            //chart.Series.Add(errorBar);

            //this.Content = chart;
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


}
