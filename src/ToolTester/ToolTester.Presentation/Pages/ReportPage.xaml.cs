using Microsoft.Maui.ApplicationModel;
using Syncfusion.Maui.Toolkit.Charts;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Pages;

public partial class ReportPage : ContentPage
{
    private readonly ReportPageModel _reportPageModel;
    private SfCartesianChart? _relatedChart;


    public ReportPage(ReportPageModel reportPageModel)
    {
        InitializeComponent();

        _reportPageModel = reportPageModel;
        BindingContext = _reportPageModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _reportPageModel.ReportDataLoaded -= OnReportDataLoaded;
        _reportPageModel.ReportDataLoaded += OnReportDataLoaded;
    }

    protected override void OnDisappearing()
    {
        _reportPageModel.ReportDataLoaded -= OnReportDataLoaded;
        base.OnDisappearing();
    }

    private void OnReportDataLoaded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(BuildCharts);
    }

    private void BuildCharts()
    {
        ChartContainer.Children.Clear();

        var relationshipPicker = new Picker
        {
            Title = "Relationship Filter",
            ItemsSource = _reportPageModel.AvailableRelationships,
            SelectedItem = _reportPageModel.SelectedRelationship
        };

        relationshipPicker.SelectedIndexChanged += (_, _) =>
        {
            _reportPageModel.SelectedRelationship =
                relationshipPicker.SelectedItem?.ToString() ?? "All";

            PopulateRelatedChart();
        };

        ChartContainer.Children.Add(BuildFalsePositiveChart());
        ChartContainer.Children.Add(relationshipPicker);
        ChartContainer.Children.Add(BuildRelatedChart());
        ChartContainer.Children.Add(BuildPolarChart());
    }

    private SfCartesianChart BuildFalsePositiveChart()
    {
        var chart = new SfCartesianChart
        {
            Title = "False Positive",
            HeightRequest = 450,
            ZoomPanBehavior = new ChartZoomPanBehavior
            {
                EnableDirectionalZooming = true,
                EnableSelectionZooming = true
            },
            Legend = new ChartLegend
            {
                IsVisible = true,
                ToggleSeriesVisibility = true
            }
        };

        chart.XAxes.Add(new NumericalAxis());
        chart.YAxes.Add(new NumericalAxis());

        foreach (var series in _reportPageModel.AggregatedSeries)
        {
            chart.Series.Add(new ScatterSeries
            {
                Label = $"Scan {series.ScanId}",
                ItemsSource = series.Items,
                XBindingPath = nameof(AggrigatedItems.CweId),
                YBindingPath = nameof(AggrigatedItems.Count),
                PointWidth = 5,
                PointHeight = 5,
                EnableTooltip = true,
                TooltipTemplate = FalsePositiveToolTip()
            });
        }

        return chart;
    }

    private SfPolarChart BuildPolarChart()
    {
        var chart = new SfPolarChart
        {
            HeightRequest = 450,
            Legend = new ChartLegend
            {
                IsVisible = true,
                ToggleSeriesVisibility = true
            },
            PrimaryAxis = new NumericalAxis(),
            SecondaryAxis = new NumericalAxis()
        };

        foreach (var series in _reportPageModel.AggregatedSeries)
        {
            chart.Series.Add(new PolarAreaSeries
            {
                Label = $"Scan {series.ScanId}",
                ItemsSource = series.Items,
                XBindingPath = nameof(AggrigatedItems.CweId),
                YBindingPath = nameof(AggrigatedItems.Count),
                ShowDataLabels = true
            });
        }

        return chart;
    }

    private SfCartesianChart BuildRelatedChart()
    {
        _relatedChart = new SfCartesianChart
        {
            Title = "Related Findings",
            HeightRequest = 450,
            ZoomPanBehavior = new ChartZoomPanBehavior
            {
                EnableDirectionalZooming = true,
                EnableSelectionZooming = true
            },
            Legend = new ChartLegend
            {
                IsVisible = true,
                ToggleSeriesVisibility = true
            }
        };

        _relatedChart.XAxes.Add(new NumericalAxis());
        _relatedChart.YAxes.Add(new NumericalAxis());

        PopulateRelatedChart();
        return _relatedChart;
    }

    private void PopulateRelatedChart()
    {
        if (_relatedChart is null)
            return;

        _relatedChart.Series.Clear();

        foreach (var series in _reportPageModel.RelatedSeries)
        {
            var sourceItems = series.Items ?? [];

            var filteredItems =
                _reportPageModel.SelectedRelationship == "All"
                    ? sourceItems
                    : sourceItems
                        .Where(item => item.Relationship ==
                                       _reportPageModel.SelectedRelationship)
                        .ToList();

            if (filteredItems.Count == 0)
                continue;

            _relatedChart.Series.Add(new BubbleSeries
            {
                Label = $"Scan {series.ScanId}",
                ItemsSource = filteredItems,
                XBindingPath = nameof(RelatedItemsInTest.GroundTruthCweId),
                YBindingPath = nameof(RelatedItemsInTest.ScannerCweId),
                SizeValuePath = nameof(RelatedItemsInTest.Count),
                EnableTooltip = true,
                TooltipTemplate = RelatedToolTip()
            });
        }
    }

    private static DataTemplate RelatedToolTip()
    {
        return new DataTemplate(() =>
        {
            var layout = CreateTooltipLayout();
            layout.Add(CreateTooltipRow("CWE:", "Item.GroundTruthCweId"));
            layout.Add(CreateTooltipRow(
                "Scanner Identified Related CWE:",
                "Item.ScannerCweId"));
            layout.Add(CreateTooltipRow("Relationship:", "Item.Relationship"));
            layout.Add(CreateTooltipRow(
                "Scanner Finding Confidence:",
                "Item.RelationshipScore"));
            layout.Add(CreateTooltipRow("Count:", "Item.Count"));
            return layout;
        });
    }

    private static DataTemplate FalsePositiveToolTip()
    {
        return new DataTemplate(() =>
        {
            var layout = CreateTooltipLayout();
            layout.Add(CreateTooltipRow("CWE:", "Item.CweId"));
            layout.Add(CreateTooltipRow("Count:", "Item.Count"));
            return layout;
        });
    }

    private static VerticalStackLayout CreateTooltipLayout()
    {
        return new VerticalStackLayout
        {
            BackgroundColor = Colors.Black,
            Padding = 6,
            Spacing = 2
        };
    }

    private static HorizontalStackLayout CreateTooltipRow(
        string caption,
        string bindingPath)
    {
        var row = new HorizontalStackLayout
        {
            BackgroundColor = Colors.Black,
            Spacing = 4
        };

        row.Add(new Label
        {
            Padding = 2,
            FontSize = 10,
            TextColor = Colors.White,
            Text = caption
        });

        var value = new Label
        {
            Padding = 2,
            FontSize = 10,
            TextColor = Colors.White
        };

        value.SetBinding(Label.TextProperty, bindingPath);
        row.Add(value);

        return row;
    }
}
