using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Collections.ObjectModel;
using ToolTester.Application.JulietCoeverages.Queries;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.Services;
using ToolTester.Presentation.Ulitlities;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Measure;

namespace ToolTester.Presentation.PageModels
{
    public partial class JulietCoveragesPageModel : BaseViewModel
    {
        private ObservableCollection<JulietCoverage> _items;
        private bool _isNavigatedTo;
        private bool _dataLoaded;
        private readonly ModalErrorHandler _errorHandler;
        private readonly IMediator _mediator;

        private IEnumerable<ISeries>? _series;
        private Axis[]? _xAxes;
        private Axis[]? _yAxes;

        public ObservableCollection<JulietCoverage> Items
        {
            get => _items;
            set => SetProperty(ref _items, value); // SetProperty handles property change notification
        }

        public IEnumerable<ISeries>? Series
        {
            get => _series;
            set => SetProperty(ref _series, value);
        }

        public Axis[]? XAxes
        {
            get => _xAxes;
            set => SetProperty(ref _xAxes, value);
        }

        public Axis[]? YAxes
        {
            get => _yAxes;
            set => SetProperty(ref _yAxes, value);
        }

        public JulietCoveragesPageModel(ModalErrorHandler errorHandler, IMediator mediator)
        {
            _errorHandler = errorHandler;
            _mediator = mediator;
            _items = new ObservableCollection<JulietCoverage>();
        }

        public async Task LoadItemsAsync()
        {

            ObservableCollection<JulietCoverage> items = new ObservableCollection<JulietCoverage>();
            var query = new GetJulietCoveragesWithPaginationQuery()
            {
                PageSize = 10000
            };
            var result = await _mediator.Send(query);

            foreach (var item in result.Items)
            {
                items.Add(new JulietCoverage()
                {
                    Id = item.Id,
                    CweId = item.CweId,
                    Covered = item.Covered

                });
            }

            Items = new ObservableCollection<JulietCoverage>(items);

            // Build LiveCharts2 series and axes from Items
            var values = Items.Select(i => (double)i.Covered).ToArray();
            Series = new ISeries[]
            {
                new ColumnSeries<double> { Values = values }
            };

            XAxes = new Axis[]
            {
                new Axis { Labels = Items.Select(i => i.CweId.ToString()).ToArray() }
            };

            YAxes = new Axis[]
            {
                new Axis()
            };
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