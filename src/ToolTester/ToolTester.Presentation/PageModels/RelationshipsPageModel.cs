using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using MediatR;
using System.Collections.ObjectModel;
using ToolTester.Application.Relationships.Queries;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.Services;
using ToolTester.Presentation.Ulitlities;

namespace ToolTester.Presentation.PageModels;

public partial class RelationshipsPageModel : BaseViewModel
{
    private readonly ModalErrorHandler _errorHandler;
    private readonly IMediator _mediator;

    private bool _isNavigatedTo;
    private bool _dataLoaded;

    [ObservableProperty]
    private ObservableCollection<Relationship> items = [];

    [ObservableProperty]
    private ObservableCollection<GroupRelations> groups = [];

    [ObservableProperty]
    private ISeries[] series = [];

    [ObservableProperty]
    private Axis[] xAxes = [];

    [ObservableProperty]
    private Axis[] yAxes = [];

    public RelationshipsPageModel(
        ModalErrorHandler errorHandler,
        IMediator mediator)
    {
        _errorHandler = errorHandler;
        _mediator = mediator;

        ConfigureAxes();
    }

    private void ConfigureAxes()
    {
        XAxes =
        [
            new Axis
            {
                Name = "CWE ID",
                MinStep = 1,
                Labeler = value => $"CWE-{value:0}"
            }
        ];

        YAxes =
        [
            new Axis
            {
                Name = "Related CWE ID",
                MinStep = 1,
                Labeler = value => $"CWE-{value:0}"
            }
        ];
    }

    public async Task LoadItemsAsync()
    {
        var query = new GetRelationshipsWithPaginationQuery
        {
            PageSize = 10000
        };

        var result = await _mediator.Send(query);

        var relationships = result.Items
            .Select(item => new Relationship
            {
                Id = item.Id,
                CWEID = item.CWEID,
                ChainId = item.ChainId,
                Nature = item.Nature,
                Oridinal = item.Oridinal,
                OrderSpecified = item.OrderSpecified,
                RelatedCweID = item.RelatedCweID
            })
            .ToList();

        Items = new ObservableCollection<Relationship>(relationships);

        Groups = new ObservableCollection<GroupRelations>(
            relationships
                .GroupBy(relationship => relationship.Nature)
                .OrderBy(group => group.Key)
                .Select(group => new GroupRelations
                {
                    Nature = group.Key ?? "Unknown",
                    Items = group
                        .OrderBy(relationship => relationship.CWEID)
                        .ThenBy(relationship => relationship.RelatedCweID)
                        .ToList()
                }));

        BuildChart();
    }

    private void BuildChart()
    {
        if (Groups.Count == 0)
        {
            Series = [];
            return;
        }

        Series = Groups
            .Where(group => group.Items.Count > 0)
            .Select(CreateScatterSeries)
            .Cast<ISeries>()
            .ToArray();
    }

    private static ScatterSeries<Relationship> CreateScatterSeries(
        GroupRelations group)
    {
        return new ScatterSeries<Relationship>
        {
            Name = group.Nature,

            Values = group.Items,

            Mapping = static (relationship, index) =>
                new Coordinate(
                    relationship.CWEID,
                    relationship.RelatedCweID),

            GeometrySize = 10,

            MinGeometrySize = 6,

            XToolTipLabelFormatter = chartPoint =>
            {
                var relationship = chartPoint.Model;

                if (relationship is null)
                {
                    return string.Empty;
                }

                return
                    $"{group.Nature}: " +
                    $"CWE-{relationship.CWEID} → " +
                    $"CWE-{relationship.RelatedCweID}";
            }
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
    private Task NavigateToProject(Relationship? relationship)
    {
        if (relationship is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync(
            $"project?id={relationship.Id}");
    }
}

public sealed class GroupRelations
{
    public string Nature { get; set; } = string.Empty;

    public List<Relationship> Items { get; set; } = [];
}