using CommunityToolkit.Mvvm.Input;

namespace ToolTester.Presentation.PageModels
{
    public partial class MainPageModel:BaseViewModel
    {
        public MainPageModel()
        {
            
        }

        [RelayCommand]
        private  Task NavigateToCatalogPage(RelationshipsPage project)
        => Shell.Current.GoToAsync("relationships");

        [RelayCommand]
        private  Task NavigateToJulietPage(JulietCoveragePage project)
        => Shell.Current.GoToAsync("julietproject");
    }
}
