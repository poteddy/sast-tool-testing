using CommunityToolkit.Mvvm.ComponentModel;

namespace ToolTester.Presentation.PageModels;

public partial class BaseViewModel : ObservableObject
{
    public INavigation Navigation { get; set; }

    [ObservableProperty]
    bool isBusy;

    [ObservableProperty]
    string title;

 
    [ObservableProperty]
    bool _isRefreshing;
}
