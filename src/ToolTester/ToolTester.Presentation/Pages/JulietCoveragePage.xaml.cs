using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Pages;

public partial class JulietCoveragePage : ContentPage
{
    private readonly object _viewModel;
    int count = 0;

    public JulietCoveragePage(JulietCoveragesPageModel julietCoveragesPageModel)
    {
        InitializeComponent();

        BindingContext = julietCoveragesPageModel;

    }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Explicitly trigger the RelayCommand on your singleton viewmodel
        if (BindingContext is PageModels.JulietCoveragesPageModel vm)
        {
            if (vm.AppearingCommand != null && vm.AppearingCommand.CanExecute(null))
            {
                vm.AppearingCommand.Execute(null);
            }
        }
    }
}