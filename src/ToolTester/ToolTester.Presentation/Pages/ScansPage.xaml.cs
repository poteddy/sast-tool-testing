using Microsoft.Maui.Controls;
using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Pages
{
    public partial class ScansPage : ContentPage
    {
        public ScansPage(ScansPageModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}