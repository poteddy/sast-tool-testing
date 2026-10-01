using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Pages;

public partial class RelationshipsPage : ContentPage
{
    public RelationshipsPage(RelationshipsPageModel catalogPageModel)
    {
        InitializeComponent();

        BindingContext = catalogPageModel;

    }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Explicitly trigger the RelayCommand on your singleton viewmodel
        if (BindingContext is PageModels.RelationshipsPageModel vm)
        {
            if (vm.AppearingCommand != null && vm.AppearingCommand.CanExecute(null))
            {
                vm.AppearingCommand.Execute(null);
            }
        }
    }
}