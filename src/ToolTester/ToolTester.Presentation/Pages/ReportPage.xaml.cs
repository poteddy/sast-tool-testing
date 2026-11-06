using ToolTester.Presentation.PageModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
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
        var theme =
           Microsoft.Maui.Controls.Application.Current?.RequestedTheme
           ?? AppTheme.Light;
        _viewModel.CurrentTheme =theme;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_viewModel.HasLoadedData)
        {
            await _viewModel.LoadReportsCommand.ExecuteAsync(null);
            var theme =
          Microsoft.Maui.Controls.Application.Current?.RequestedTheme
          ?? AppTheme.Light;
            _viewModel.CurrentTheme = theme;
        }
    }
}