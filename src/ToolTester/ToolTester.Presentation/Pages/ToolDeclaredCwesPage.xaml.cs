using Syncfusion.Maui.Toolkit.Carousel;
using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Pages;

public partial class ToolDeclaredCwesPage : ContentPage
{
	public ToolDeclaredCwesPage(ToolDeclaredCwesPageModel viewModel)
    {
		InitializeComponent();
        BindingContext = viewModel;
        // expose page reference for commands in XAML (Remove button)
        this.SetValue(VisualElement.ClassIdProperty, "ThisPage");
    }
}