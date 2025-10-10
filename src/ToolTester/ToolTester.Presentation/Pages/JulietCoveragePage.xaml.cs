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

}