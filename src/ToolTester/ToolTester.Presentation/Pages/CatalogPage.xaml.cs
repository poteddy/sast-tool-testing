using Syncfusion.Maui.DataGrid;
using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Pages;

public partial class CatalogPage : ContentPage
{
    private readonly object _viewModel;
    int count = 0;

    public CatalogPage(CatalogPageModel catalogPageModel)
    {
        InitializeComponent();

        BindingContext = catalogPageModel;
   
    }
   
}