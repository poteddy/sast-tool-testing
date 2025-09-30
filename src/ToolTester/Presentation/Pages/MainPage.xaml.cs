using ToolTester.Presentation.Models;
using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}