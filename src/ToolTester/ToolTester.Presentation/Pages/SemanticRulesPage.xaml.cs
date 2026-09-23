using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Pages;

public partial class SemanticRulesPage : ContentPage
{
	public SemanticRulesPage(SemanticRulesViewModel SmPageModel)
    {
        InitializeComponent();
        BindingContext = SmPageModel;
    }
}