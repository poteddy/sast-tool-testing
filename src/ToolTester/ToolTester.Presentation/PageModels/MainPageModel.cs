using CommunityToolkit.Mvvm.Input;

namespace ToolTester.Presentation.PageModels
{
    public partial class MainPageModel:BaseViewModel
    {
        public MainPageModel()
        {
            
        }

        [RelayCommand]
        private  Task NavigateToCatalogPage(CatalogPage project)
        => Shell.Current.GoToAsync("catalog");
    }
}
