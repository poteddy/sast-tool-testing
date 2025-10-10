using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Pages;

public partial class RelationshipsPage : ContentPage
{
	public RelationshipsPage(RelationshipsPageModel catalogPageModel)
    {
        InitializeComponent();

        BindingContext = catalogPageModel;

    }
}