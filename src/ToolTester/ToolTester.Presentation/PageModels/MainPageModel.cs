using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Models;
using ToolTester.Infrastructure.Persistance;
using ToolTester.Presentation.Services;

namespace ToolTester.Presentation.PageModels
{
    public partial class MainPageModel:BaseViewModel
    {
        private readonly IConfiguration _configuration;
       private readonly IApplicationDbContextSeed _applicationDbContextSeed;
        private readonly ModalErrorHandler _errorHandler;
        private bool _isNavigatedTo;
        private bool _dataLoaded;
        public MainPageModel(IConfiguration configuration,IApplicationDbContextSeed applicationDbContextSeed, ModalErrorHandler errorHandler)
        {
            _configuration = configuration;
            _applicationDbContextSeed = applicationDbContextSeed;
           _errorHandler = errorHandler;
        }
        private async Task InitData()
        {
            var JulietzipPath = _configuration.GetRequiredSection("JulietProjectSetting").Get<JulietProjectSetting>().Path;
            if (!File.Exists(JulietzipPath))
            {
                //do counts here
                Console.WriteLine();
            }


       
            _applicationDbContextSeed.SeedCWECatalog();
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
                await InitData();
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
              //  await LoadData();
            }
            catch (Exception e)
            {
                _errorHandler.HandleError(e);
            }
            finally
            {
                IsRefreshing = false;
            }
        }
        [RelayCommand]
        private  Task NavigateToCatalogPage(RelationshipsPage project)
        => Shell.Current.GoToAsync("relationships");

        [RelayCommand]
        private  Task NavigateToJulietPage(JulietCoveragePage project)
        => Shell.Current.GoToAsync("julietproject");
    }
}
