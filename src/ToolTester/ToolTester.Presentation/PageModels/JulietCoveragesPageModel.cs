
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Collections.ObjectModel;
using ToolTester.Application.JulietCoeverages.Queries;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.Services;
using ToolTester.Presentation.Ulitlities;

namespace ToolTester.Presentation.PageModels
{
    public partial class JulietCoveragesPageModel : BaseViewModel
    {
        private ObservableCollection<JulietCoverage> _items;
        private bool _isNavigatedTo;
        private bool _dataLoaded;
        private readonly ModalErrorHandler _errorHandler;
        private readonly IMediator _mediator;

         public ObservableCollection<JulietCoverage> Items
        {
            get => _items;
            set => SetProperty(ref _items, value); // SetProperty handles property change notification
        }
        public JulietCoveragesPageModel(ModalErrorHandler errorHandler, IMediator mediator)
        {
            _errorHandler = errorHandler;
            _mediator = mediator;
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
                        CWE_ID = item.CWE_ID,
                         Covered = item.Covered
                      
                    });
                }
            
            Items = new ObservableCollection<JulietCoverage>(items);
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
