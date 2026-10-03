using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Pages;

public partial class ReportPage : ContentPage
{
    private readonly ReportPageModel _viewModel;

    public ReportPage(
        ReportPageModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_viewModel.HasLoadedData)
        {
            await _viewModel.LoadReportsCommand.ExecuteAsync(null);
        }
    }
}