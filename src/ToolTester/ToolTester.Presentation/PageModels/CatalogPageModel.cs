using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Domain.Entities;
using ToolTester.Presentation.Interfaces;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.Services;
using ToolTester.Presentation.Ulitlities;

namespace ToolTester.Presentation.PageModels;
public partial class CatalogPageModel : BaseViewModel // Assuming you have ObservableObject for property change notification
{


    private ObservableCollection<CweCatalog> _items;
    private bool _isNavigatedTo;
    private bool _dataLoaded;
    private readonly ModalErrorHandler _errorHandler;

    public ObservableCollection<CweCatalog> Items
    {
        get => _items;
        set => SetProperty(ref _items, value); // SetProperty handles property change notification
    }

    public CatalogPageModel(ModalErrorHandler errorHandler)
    {
        _errorHandler = errorHandler;
    }
    public async Task LoadItemsAsync()
    {
       
        ObservableCollection<CweCatalog> cWECatalogs = new ObservableCollection<CweCatalog>();
        cWECatalogs.Add(new CweCatalog() { Name = "hello do i work" });
        Items = new ObservableCollection<CweCatalog>(cWECatalogs);
    }

    //[RelayCommand]
    //private async Task GoToAuthor(CweCatalog? author)
    //{
    //    if (author is null)
    //    {
    //        return;
    //    }

    //    // Very testable 😘
    //    await Shell.Current.GoToAsync(nameof(CweCatalogDetailsPage), true, new Dictionary<string, object>
    //    {
    //        { "Author", author }
    //    }
    //    ); 
    //}

    [RelayCommand]
    private void NavigatedTo() =>
    _isNavigatedTo = true;

    [RelayCommand]
    private void NavigatedFrom() =>
        _isNavigatedTo = false;


    [RelayCommand]
    private async Task Appearing()
    {
        if (!_dataLoaded)
        {
           
            //await InitData(_seedDataService);
            _dataLoaded = true;
            await Refresh();
        }
        // This means we are being navigated to
        else if (!_isNavigatedTo)
        {
            await Refresh();
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        try
        {
            IsRefreshing = true;
            LoadItemsAsync().FireAndForgetSafeAsync(_errorHandler); 
        }
        catch (Exception e)
        {

        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task AddTask()
          => Shell.Current.GoToAsync($"task");

    [RelayCommand]
    private Task NavigateToProject(CweCatalogDetailsPage project)
        => Shell.Current.GoToAsync($"project?id={project.ID}");


}
