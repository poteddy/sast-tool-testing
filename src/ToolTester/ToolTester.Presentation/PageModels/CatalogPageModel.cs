using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MediatR;
using System.Collections.ObjectModel;
using ToolTester.Application.CWECatalogs.Queries;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.Services;
using ToolTester.Presentation.Ulitlities;

namespace ToolTester.Presentation.PageModels;

public partial class CatalogPageModel : BaseViewModel
{
    private readonly ModalErrorHandler _errorHandler;
    private readonly IMediator _mediator;

    private bool _isNavigatedTo;
    private bool _dataLoaded;

    [ObservableProperty]
    private ObservableCollection<CweCatalog> items = [];

    [ObservableProperty]
    private ISeries[] series = [];

    [ObservableProperty]
    private Axis[] xAxes = [];

    [ObservableProperty]
    private Axis[] yAxes = [];

    public CatalogPageModel(
        ModalErrorHandler errorHandler,
        IMediator mediator)
    {
        _errorHandler = errorHandler;
        _mediator = mediator;

        ConfigureEmptyChart();
    }

    private void ConfigureEmptyChart()
    {
        Series =
        [
            new ColumnSeries<double>
            {
                Name = "Status",
                Values = []
            }
        ];

        XAxes =
        [
            new Axis
            {
                Name = "CWE",
                Labels = [],
                LabelsRotation = 90
            }
        ];

        YAxes =
        [
            new Axis
            {
                Name = "Status",
                MinLimit = 0
            }
        ];
    }

    private async Task LoadItemsAsync()
    {
        var result = await _mediator.Send(
            new GetCweCatalogsWithPaginationQuery());

        Items = new ObservableCollection<CweCatalog>(
            result.Items.Select(catalog => new CweCatalog
            {
                Id = catalog.Id,
                Name = catalog.Name,
                Abstraction = catalog.Abstraction,
                Description = catalog.Description,
                Status = catalog.Status
            }));

        BuildChart();
    }

    private void BuildChart()
    {
        if (Items.Count == 0)
        {
            ConfigureEmptyChart();
            return;
        }

        var chartItems = Items
            .OrderBy(item => item.Id)
            .ToArray();

        Series =
        [
            new ColumnSeries<double>
            {
                Name = "Status",
                Values = chartItems
                    .Select(item => ConvertStatusToNumber(item.Status))
                    .ToArray()
            }
        ];

        XAxes =
        [
            new Axis
            {
                Name = "CWE",
                Labels = chartItems
                    .Select(item => $"CWE-{item.Id}")
                    .ToArray(),

                LabelsRotation = 90
            }
        ];

        YAxes =
        [
            new Axis
            {
                Name = "Status",
                MinLimit = 0,
                MinStep = 1
            }
        ];
    }

    private static double ConvertStatusToNumber(object? status)
    {
        if (status is null)
        {
            return 0;
        }

        if (status.GetType().IsEnum)
        {
            return Convert.ToDouble(status);
        }

        return status switch
        {
            byte value => value,
            short value => value,
            int value => value,
            long value => value,
            float value => value,
            double value => value,
            decimal value => (double)value,
            bool value => value ? 1 : 0,
            _ when double.TryParse(
                status.ToString(),
                out var parsedValue) => parsedValue,
            _ => 0
        };
    }

    [RelayCommand]
    private void NavigatedTo()
    {
        _isNavigatedTo = true;
    }

    [RelayCommand]
    private void NavigatedFrom()
    {
        _isNavigatedTo = false;
    }

    [RelayCommand]
    private async Task Appearing()
    {
        if (!_dataLoaded)
        {
            await Refresh();
            _dataLoaded = true;
            return;
        }

        if (!_isNavigatedTo)
        {
            await Refresh();
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        if (IsRefreshing)
        {
            return;
        }

        try
        {
            IsRefreshing = true;
            await LoadItemsAsync();
        }
        catch (Exception exception)
        {
            Task.FromException(exception)
            .FireAndForgetSafeAsync(_errorHandler);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task AddTask()
    {
        return Shell.Current.GoToAsync("task");
    }

    [RelayCommand]
    private Task NavigateToProject(CweCatalog? catalog)
    {
        if (catalog is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync(
            $"project?id={catalog.Id}");
    }
}