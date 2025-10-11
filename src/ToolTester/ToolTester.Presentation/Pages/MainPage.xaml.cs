using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Pages
{
  
    public partial class MainPage : ContentPage
    {
       
        public MainPage(MainPageModel  mainPageModel)
        {
            InitializeComponent();
            BindingContext = mainPageModel;
        }

      
    }
}
