using Microsoft.Maui.ApplicationModel;
using Syncfusion.Maui.Toolkit.Charts;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Pages;

public partial class ReportPage : ContentPage
{
    private const string FalseNegativeAxisName =
        "FalseNegativeAxis";

    private const string CumulativePercentAxisName =
        "CumulativePercentAxis";

    private const double MaximumChartWidth = 1400;

    private readonly ReportPageModel _reportPageModel;

    private SfCartesianChart? _relatedChart;

    public ReportPage(
        ReportPageModel reportPageModel)
    {
        InitializeComponent();

        _reportPageModel = reportPageModel;
        BindingContext = _reportPageModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _reportPageModel.ReportDataLoaded -=
            OnReportDataLoaded;

        _reportPageModel.ReportDataLoaded +=
            OnReportDataLoaded;

        if (_reportPageModel.HasLoadedData)
        {
            MainThread.BeginInvokeOnMainThread(
                BuildCharts);
        }
    }

    protected override void OnDisappearing()
    {
        _reportPageModel.ReportDataLoaded -=
            OnReportDataLoaded;

        base.OnDisappearing();
    }

    private void OnReportDataLoaded(
        object? sender,
        EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(
            BuildCharts);
    }

    private void BuildCharts()
    {
        ChartContainer.Children.Clear();

        ChartContainer.Children.Add(
            CreateChartSection(
                BuildFalsePositiveChart()));

        ChartContainer.Children.Add(
            CreateRelationshipFilter());

        ChartContainer.Children.Add(
            CreateChartSection(
                BuildRelatedChart()));

        AddFalseNegativeParetoCharts();

        ChartContainer.Children.Add(
            CreateChartSection(
                CreateRelationshipMixChart()));

        ChartContainer.Children.Add(
            CreateChartSection(
                BuildPolarChart(),
                includeZoomControls: false));
    }

    private View CreateRelationshipFilter()
    {
        var relationshipPicker = new Picker
        {
            Title = "Relationship Filter",
            ItemsSource =
                _reportPageModel.AvailableRelationships,
            SelectedItem =
                _reportPageModel.SelectedRelationship,
            HorizontalOptions =
                LayoutOptions.Fill,
            //MaximumWidthRequest =
            //    MaximumChartWidth
        };

        relationshipPicker.SelectedIndexChanged +=
            (_, _) =>
            {
                _reportPageModel.SelectedRelationship =
                    relationshipPicker.SelectedItem?
                        .ToString()
                    ?? "All";

                PopulateRelatedChart();
            };

        return new Border
        {
            //HorizontalOptions =
            //    LayoutOptions.Center,
            //MaximumWidthRequest =
            //    MaximumChartWidth,
            Stroke =
                Brush.Transparent,
            Padding =
                new Thickness(12, 4),
            Content =
                relationshipPicker
        };
    }

    private static ChartZoomPanBehavior
        CreateZoomBehavior()
    {
        return new ChartZoomPanBehavior
        {
            /*
             * Keep this true.
             *
             * Syncfusion uses mouse-wheel zoom when
             * EnablePinchZooming is false. Keeping it
             * true prevents normal mouse-wheel scrolling
             * from unintentionally zooming the chart.
             */
            EnablePinchZooming = true,

            /*
             * Desktop users can drag a rectangle directly
             * over the chart to zoom into an area.
             */
            EnableSelectionZooming = true,

            /*
             * Once zoomed, users can drag the chart to
             * move through the visible range.
             */
            EnablePanning = true,

            /*
             * Directional zooming depends on pinch
             * direction. XY provides predictable behavior
             * for toolbar and selection zooming.
             */
            EnableDirectionalZooming = false,
            EnableDoubleTap = false,
            ZoomMode = ZoomMode.XY,

            /*
             * Prevent users from zooming so deeply that
             * they lose context.
             */
            MaximumZoomLevel = 20,

            SelectionRectFill =
                new SolidColorBrush(
                    Color.FromArgb("#3380BFFF")),

            SelectionRectStroke =
                new SolidColorBrush(
                    Colors.DodgerBlue),

            SelectionRectStrokeWidth = 2
        };
    }

    private static View CreateChartSection(
        SfCartesianChart chart,
        bool includeZoomControls = true)
    {
        var content = new VerticalStackLayout
        {
            Spacing = 6,
            HorizontalOptions =
                LayoutOptions.Fill
        };

        if (includeZoomControls &&
            chart.ZoomPanBehavior is not null)
        {
            content.Add(
                CreateChartToolbar(
                    chart.ZoomPanBehavior));
        }

        content.Add(chart);

        return new Border
        {
            //HorizontalOptions =
            //    LayoutOptions.Center,
            //MaximumWidthRequest =
            //    MaximumChartWidth,
            Stroke =
                new SolidColorBrush(
                    Color.FromArgb("#D9D9D9")),
            StrokeThickness = 1,
            Padding = 10,
            Margin =
                new Thickness(8, 6),
            Content = content
        };
    }

    private static View CreateChartSection(
        SfPolarChart chart,
        bool includeZoomControls)
    {
        return new Border
        {
            //HorizontalOptions =
            //    LayoutOptions.Center,
            //MaximumWidthRequest =
            //    MaximumChartWidth,
            Stroke =
                new SolidColorBrush(
                    Color.FromArgb("#D9D9D9")),
            StrokeThickness = 1,
            Padding = 10,
            Margin =
                new Thickness(8, 6),
            Content = chart
        };
    }

    private static Grid CreateChartToolbar(
        ChartZoomPanBehavior zoomBehavior)
    {
        var toolbar = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(
                    GridLength.Star),

                new ColumnDefinition(
                    GridLength.Auto),

                new ColumnDefinition(
                    GridLength.Auto),

                new ColumnDefinition(
                    GridLength.Auto)
            },

            ColumnSpacing = 6,
            HorizontalOptions =
                LayoutOptions.Fill
        };

        var instructions = new Label
        {
            Text =
                "Mouse wheel scrolls the report. " +
                "Drag on the chart to select a zoom area.",
            FontSize = 12,
            TextColor =
                Colors.Gray,
            VerticalTextAlignment =
                TextAlignment.Center,
            LineBreakMode =
                LineBreakMode.TailTruncation
        };

        var zoomInButton =
            CreateToolbarButton(
                "Zoom In",
                "Zoom in");

        var zoomOutButton =
            CreateToolbarButton(
                "Zoom Out",
                "Zoom out");

        var resetButton =
            CreateToolbarButton(
                "Reset",
                "Reset zoom");

        zoomInButton.Clicked +=
            (_, _) =>
                zoomBehavior.ZoomIn();

        zoomOutButton.Clicked +=
            (_, _) =>
                zoomBehavior.ZoomOut();

        resetButton.Clicked +=
            (_, _) =>
                zoomBehavior.Reset();

        Grid.SetColumn(
            instructions,
            0);

        Grid.SetColumn(
            zoomInButton,
            1);

        Grid.SetColumn(
            zoomOutButton,
            2);

        Grid.SetColumn(
            resetButton,
            3);

        toolbar.Children.Add(
            instructions);

        toolbar.Children.Add(
            zoomInButton);

        toolbar.Children.Add(
            zoomOutButton);

        toolbar.Children.Add(
            resetButton);

        return toolbar;
    }

    private static Button CreateToolbarButton(
        string text,
        string semanticDescription)
    {
        var button = new Button
        {
            Text = text,
            FontSize = 12,
            Padding =
                new Thickness(12, 6),
            MinimumHeightRequest = 36,
            VerticalOptions =
                LayoutOptions.Center
        };

        SemanticProperties.SetDescription(
            button,
            semanticDescription);

        return button;
    }

    private void AddFalseNegativeParetoCharts()
    {
        if (_reportPageModel
            .FalseNegativeParetoSeries
            .Count == 0)
        {
            ChartContainer.Children.Add(
                new Label
                {
                    Text =
                        "No false-negative data is available.",
                    FontSize = 14,
                    HorizontalTextAlignment =
                        TextAlignment.Center,
                    HorizontalOptions =
                        LayoutOptions.Fill,
                    Margin =
                        new Thickness(10)
                });

            return;
        }

        foreach (var scannerSeries in
                 _reportPageModel
                     .FalseNegativeParetoSeries)
        {
            if (scannerSeries.Items.Count == 0)
            {
                continue;
            }

            ChartContainer.Children.Add(
                CreateChartSection(
                    BuildFalseNegativeParetoChart(
                        scannerSeries)));
        }
    }

    private static SfCartesianChart
        BuildFalseNegativeParetoChart(
            FalseNegativeParetoSeries scannerSeries)
    {
        var chart = new SfCartesianChart
        {
            Title =
                $"False Negatives Pareto - " +
                $"{scannerSeries.ScannerName}",

            HeightRequest = 500,

            HorizontalOptions =
                LayoutOptions.Fill,

            ZoomPanBehavior =
                CreateZoomBehavior(),

            Legend =
                new ChartLegend
                {
                    IsVisible = true,
                    ToggleSeriesVisibility = true
                }
        };

        var cweAxis = new CategoryAxis
        {
            Title =
                new ChartAxisTitle
                {
                    Text =
                        "Juliet Ground-Truth CWE"
                },

            ShowMajorGridLines = false,
            LabelRotation = -45
        };

        var falseNegativeAxis =
            new NumericalAxis
            {
                Name =
                    FalseNegativeAxisName,

                Title =
                    new ChartAxisTitle
                    {
                        Text =
                            "False Negatives"
                    },

                Minimum = 0,
                ShowMajorGridLines = true
            };

        var cumulativePercentAxis =
            new NumericalAxis
            {
                Name =
                    CumulativePercentAxisName,

                Title =
                    new ChartAxisTitle
                    {
                        Text =
                            "Cumulative Percentage"
                    },

                Minimum = 0,
                Maximum = 100,
                Interval = 20,
                CrossesAt = double.MaxValue,
                ShowMajorGridLines = false
            };

        chart.XAxes.Add(
            cweAxis);

        chart.YAxes.Add(
            falseNegativeAxis);

        chart.YAxes.Add(
            cumulativePercentAxis);

        var falseNegativeColumns =
            new ColumnSeries
            {
                Label =
                    "False Negatives",

                ItemsSource =
                    scannerSeries.Items,

                XBindingPath =
                    nameof(
                        FalseNegativeChartPoint
                            .CweLabel),

                YBindingPath =
                    nameof(
                        FalseNegativeChartPoint
                            .FalseNegatives),

                YAxisName =
                    FalseNegativeAxisName,

                EnableTooltip = true,
                ShowDataLabels = true,

                TooltipTemplate =
                    FalseNegativeToolTip()
            };

        var cumulativeLine =
            new LineSeries
            {
                Label =
                    "Cumulative %",

                ItemsSource =
                    scannerSeries.Items,

                XBindingPath =
                    nameof(
                        FalseNegativeChartPoint
                            .CweLabel),

                YBindingPath =
                    nameof(
                        FalseNegativeChartPoint
                            .CumulativePercent),

                YAxisName =
                    CumulativePercentAxisName,

                EnableTooltip = true,
                ShowMarkers = true,

                TooltipTemplate =
                    CumulativePercentToolTip()
            };

        chart.Series.Add(
            falseNegativeColumns);

        chart.Series.Add(
            cumulativeLine);

        return chart;
    }

    private SfCartesianChart
        BuildFalsePositiveChart()
    {
        var chart = new SfCartesianChart
        {
            Title =
                "False Positive",

            HeightRequest = 450,

            HorizontalOptions =
                LayoutOptions.Fill,

            ZoomPanBehavior =
                CreateZoomBehavior(),

            Legend =
                new ChartLegend
                {
                    IsVisible = true,
                    ToggleSeriesVisibility = true
                }
        };

        chart.XAxes.Add(
            new NumericalAxis
            {
                Title =
                    new ChartAxisTitle
                    {
                        Text =
                            "Scanner CWE"
                    },

                ShowMajorGridLines = true
            });

        chart.YAxes.Add(
            new NumericalAxis
            {
                Title =
                    new ChartAxisTitle
                    {
                        Text =
                            "Finding Count"
                    },

                Minimum = 0,
                ShowMajorGridLines = true
            });

        foreach (var series in
                 _reportPageModel.AggregatedSeries)
        {
            chart.Series.Add(
                new ScatterSeries
                {
                    Label =
                        $"Scan {series.ScanName}",

                    ItemsSource =
                        series.Items,

                    XBindingPath =
                        nameof(
                            AggrigatedItems.CweId),

                    YBindingPath =
                        nameof(
                            AggrigatedItems.Count),

                    PointWidth = 7,
                    PointHeight = 7,

                    EnableTooltip = true,

                    TooltipTemplate =
                        FalsePositiveToolTip()
                });
        }

        return chart;
    }

    private SfCartesianChart
        BuildRelatedChart()
    {
        _relatedChart =
            new SfCartesianChart
            {
                Title =
                    "Related Findings",

                HeightRequest = 450,

                HorizontalOptions =
                    LayoutOptions.Fill,

                ZoomPanBehavior =
                    CreateZoomBehavior(),

                Legend =
                    new ChartLegend
                    {
                        IsVisible = true,
                        ToggleSeriesVisibility = true
                    }
            };

        _relatedChart.XAxes.Add(
            new NumericalAxis
            {
                Title =
                    new ChartAxisTitle
                    {
                        Text =
                            "Ground Truth CWE"
                    },

                ShowMajorGridLines = true
            });

        _relatedChart.YAxes.Add(
            new NumericalAxis
            {
                Title =
                    new ChartAxisTitle
                    {
                        Text =
                            "Scanner CWE"
                    },

                ShowMajorGridLines = true
            });

        PopulateRelatedChart();

        return _relatedChart;
    }

    private void PopulateRelatedChart()
    {
        if (_relatedChart is null)
        {
            return;
        }

        _relatedChart.Series.Clear();

        foreach (var series in
                 _reportPageModel.RelatedSeries)
        {
            var sourceItems =
                series.Items ?? [];

            var filteredItems =
                _reportPageModel
                    .SelectedRelationship == "All"
                    ? sourceItems
                    : sourceItems
                        .Where(
                            item =>
                                string.Equals(
                                    item.Relationship,
                                    _reportPageModel
                                        .SelectedRelationship,
                                    StringComparison
                                        .OrdinalIgnoreCase))
                        .ToList();

            if (filteredItems.Count == 0)
            {
                continue;
            }

            _relatedChart.Series.Add(
                new BubbleSeries
                {
                    Label =
                        $"Scan {series.ScanName}",

                    ItemsSource =
                        filteredItems,

                    XBindingPath =
                        nameof(
                            RelatedItemsInTest
                                .GroundTruthCweId),

                    YBindingPath =
                        nameof(
                            RelatedItemsInTest
                                .ScannerCweId),

                    SizeValuePath =
                        nameof(
                            RelatedItemsInTest
                                .Count),

                    EnableTooltip = true,

                    TooltipTemplate =
                        RelatedToolTip()
                });
        }
    }

    private SfCartesianChart
        CreateRelationshipMixChart()
    {
        var chart = new SfCartesianChart
        {
            Title =
                "Relationship Mix by Scanner",

            HeightRequest = 550,

            HorizontalOptions =
                LayoutOptions.Fill,

            ZoomPanBehavior =
                CreateZoomBehavior(),

            Legend =
                new ChartLegend
                {
                    IsVisible = true,
                    ToggleSeriesVisibility = true
                }
        };

        chart.XAxes.Add(
            new CategoryAxis
            {
                Title =
                    new ChartAxisTitle
                    {
                        Text = "Scanner"
                    },

                ShowMajorGridLines = false
            });

        chart.YAxes.Add(
            new NumericalAxis
            {
                Title =
                    new ChartAxisTitle
                    {
                        Text =
                            "Cumulative Relationship Mix"
                    },

                Minimum = 0,
                Maximum = 100,
                Interval = 10,
                ShowMajorGridLines = true
            });

        AddRelationshipRangeSeries(
            chart,
            "Exact",
            Colors.SteelBlue);

        AddRelationshipRangeSeries(
            chart,
            "DirectSibling",
            Colors.ForestGreen);

        AddRelationshipRangeSeries(
            chart,
            "SameRootCauseBroaderCwe",
            Colors.LightBlue);

        AddRelationshipRangeSeries(
            chart,
            "DirectParent",
            Colors.Gold);

        AddRelationshipRangeSeries(
            chart,
            "DirectChild",
            Colors.DarkOrange);

        AddRelationshipRangeSeries(
            chart,
            "SharedAncestor",
            Colors.Gray);

        AddRelationshipRangeSeries(
            chart,
            "CanPrecede",
            Colors.Purple);

        AddRelationshipRangeSeries(
            chart,
            "Unrelated",
            Colors.Red);

        return chart;
    }

    private void AddRelationshipRangeSeries(
        SfCartesianChart chart,
        string relationship,
        Color color)
    {
        var items =
            _reportPageModel
                .RelationshipMixChartData
                .Where(
                    item =>
                        string.Equals(
                            item.Relationship,
                            relationship,
                            StringComparison
                                .OrdinalIgnoreCase))
                .ToList();

        if (items.Count == 0)
        {
            return;
        }

        chart.Series.Add(
            new RangeColumnSeries
            {
                Label =
                    relationship,

                ItemsSource =
                    items,

                XBindingPath =
                    nameof(
                        RelationshipMixChartPoint
                            .ScannerName),

                Low =
                    nameof(
                        RelationshipMixChartPoint
                            .Low),

                High =
                    nameof(
                        RelationshipMixChartPoint
                            .High),

                Fill =
                    new SolidColorBrush(
                        color),

                EnableTooltip = true,

                TooltipTemplate =
                    RelationshipMixToolTip()
            });
    }

    private SfPolarChart BuildPolarChart()
    {
        var chart = new SfPolarChart
        {
            Title =
                "Scanner CWE Distribution",

            HeightRequest = 450,

            HorizontalOptions =
                LayoutOptions.Fill,

            Legend =
                new ChartLegend
                {
                    IsVisible = true,
                    ToggleSeriesVisibility = true
                },

            PrimaryAxis =
                new NumericalAxis
                {
                    Title =
                        new ChartAxisTitle
                        {
                            Text =
                                "Scanner CWE"
                        }
                },

            SecondaryAxis =
                new NumericalAxis
                {
                    Title =
                        new ChartAxisTitle
                        {
                            Text =
                                "Finding Count"
                        },

                    Minimum = 0
                }
        };

        foreach (var series in
                 _reportPageModel.AggregatedSeries)
        {
            chart.Series.Add(
                new PolarAreaSeries
                {
                    Label =
                        $"Scan {series.ScanName}",

                    ItemsSource =
                        series.Items,

                    XBindingPath =
                        nameof(
                            AggrigatedItems.CweId),

                    YBindingPath =
                        nameof(
                            AggrigatedItems.Count),

                    ShowDataLabels = true
                });
        }

        return chart;
    }

    private static DataTemplate
        FalseNegativeToolTip()
    {
        return new DataTemplate(
            () =>
            {
                var layout =
                    CreateTooltipLayout();

                layout.Add(
                    CreateTooltipRow(
                        "Scan id:",
                        "Item.ScanId"));

                layout.Add(
                    CreateTooltipRow(
                        "Ground Truth CWE:",
                        "Item.CweLabel"));

                layout.Add(
                    CreateTooltipRow(
                        "CWE Name:",
                        "Item.CweName"));

                layout.Add(
                    CreateTooltipRow(
                        "Known Opportunities:",
                        "Item.Opportunities"));

                layout.Add(
                    CreateTooltipRow(
                        "Detected:",
                        "Item.Detected"));

                layout.Add(
                    CreateTooltipRow(
                        "False Negatives:",
                        "Item.FalseNegatives"));

                return layout;
            });
    }

    private static DataTemplate
        CumulativePercentToolTip()
    {
        return new DataTemplate(
            () =>
            {
                var layout =
                    CreateTooltipLayout();

                layout.Add(
                    CreateTooltipRow(
                        "Scanner:",
                        "Item.ScannerName"));

                layout.Add(
                    CreateTooltipRow(
                        "Ground Truth CWE:",
                        "Item.CweLabel"));

                layout.Add(
                    CreateTooltipRow(
                        "Cumulative %:",
                        "Item.CumulativePercent",
                        "{0:F2}%"));

                return layout;
            });
    }

    private static DataTemplate
        RelatedToolTip()
    {
        return new DataTemplate(
            () =>
            {
                var layout =
                    CreateTooltipLayout();

                layout.Add(
                    CreateTooltipRow(
                        "Ground Truth CWE:",
                        "Item.GroundTruthCweId"));

                layout.Add(
                    CreateTooltipRow(
                        "Scanner Identified Related CWE:",
                        "Item.ScannerCweId"));

                layout.Add(
                    CreateTooltipRow(
                        "Relationship:",
                        "Item.Relationship"));

                layout.Add(
                    CreateTooltipRow(
                        "Scanner Finding Confidence:",
                        "Item.RelationshipScore"));

                layout.Add(
                    CreateTooltipRow(
                        "Count:",
                        "Item.Count"));

                return layout;
            });
    }

    private static DataTemplate
        FalsePositiveToolTip()
    {
        return new DataTemplate(
            () =>
            {
                var layout =
                    CreateTooltipLayout();

                layout.Add(
                    CreateTooltipRow(
                        "CWE:",
                        "Item.CweId"));

                layout.Add(
                    CreateTooltipRow(
                        "Count:",
                        "Item.Count"));

                return layout;
            });
    }

    private static DataTemplate
        RelationshipMixToolTip()
    {
        return new DataTemplate(
            () =>
            {
                var layout =
                    CreateTooltipLayout();

                layout.Add(
                    CreateTooltipRow(
                        "Scanner:",
                        "Item.ScannerName"));

                layout.Add(
                    CreateTooltipRow(
                        "Relationship:",
                        "Item.Relationship"));

                layout.Add(
                    CreateTooltipRow(
                        "Count:",
                        "Item.Count"));

                layout.Add(
                    CreateTooltipRow(
                        "Percentage:",
                        "Item.Percentage",
                        "{0:F2}%"));

                layout.Add(
                    CreateTooltipRow(
                        "Range From:",
                        "Item.Low",
                        "{0:F2}%"));

                layout.Add(
                    CreateTooltipRow(
                        "Range To:",
                        "Item.High",
                        "{0:F2}%"));

                return layout;
            });
    }

    private static VerticalStackLayout
        CreateTooltipLayout()
    {
        return new VerticalStackLayout
        {
            BackgroundColor =
                Colors.Black,

            Padding = 8,
            Spacing = 3
        };
    }

    private static HorizontalStackLayout
        CreateTooltipRow(
            string caption,
            string bindingPath,
            string? stringFormat = null)
    {
        var row =
            new HorizontalStackLayout
            {
                BackgroundColor =
                    Colors.Black,

                Spacing = 4
            };

        row.Add(
            new Label
            {
                Padding = 2,
                FontSize = 11,
                FontAttributes =
                    FontAttributes.Bold,
                TextColor =
                    Colors.White,
                Text = caption
            });

        var value =
            new Label
            {
                Padding = 2,
                FontSize = 11,
                TextColor =
                    Colors.White
            };

        value.SetBinding(
            Label.TextProperty,
            new Binding(
                bindingPath,
                stringFormat:
                    stringFormat));

        row.Add(value);

        return row;
    }
}