using Syncfusion.Maui.DataGrid.Helper;
using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Pages;

public partial class ParsePage : ContentPage
{
	public ParsePage(ParsePageModel parsePageModel)
	{
		InitializeComponent();
        BindingContext = parsePageModel;

    }
   
}