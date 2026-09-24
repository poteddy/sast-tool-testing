using MediatR;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Compatibility;
using Syncfusion.Maui.Toolkit.Charts;
using System.Collections.ObjectModel;
using ToolTester.Domain.Entities;
using ToolTester.Presentation.Models;
using ToolTester.Presentation.PageModels;
using ToolTester.Presentation.Services;
using ToolTester.Presentation.Ulitlities;

namespace ToolTester.Presentation.Pages;

public partial class ReportPage : ContentPage
{
    private readonly ReportPageModel _reportPageModel;
    private SfCartesianChart? _relatedChart;

    public ReportPage(ReportPageModel reportPageModel)
    {
        InitializeComponent();
        _reportPageModel = reportPageModel;
        _reportPageModel.LoadItemsAsync().FireAndForgetSafeAsync();
        BindingContext = _reportPageModel;

        var relationshipPicker = new Picker
        {
            Title = "Relationship Filter"
        };

        relationshipPicker.SetBinding(
            Picker.ItemsSourceProperty,
            nameof(ReportPageModel.AvailableRelationships));

        relationshipPicker.SelectedIndex = 0;

        relationshipPicker.SelectedIndexChanged += (_, _) =>
        {
            _reportPageModel.SelectedRelationship =
                relationshipPicker.SelectedItem?.ToString() ?? "All";

            BuildRelatedChartWithFilter();
        };
        Microsoft.Maui.Controls.StackLayout layout = new Microsoft.Maui.Controls.StackLayout
        {
            Children =
            {
               BuildFalsePChart(),
               relationshipPicker,
               BuildRelatedChart(),
               BuildPolarChart()
            }
        };
        Content = new ScrollView
        {
            Content = layout
        };

    }
    private void BuildRelatedChartWithFilter()
    {
        PopulateRelatedChart();
    }
    private SfCartesianChart BuildFalsePChart()
    {

        SfCartesianChart FalsePChart = new SfCartesianChart();
        FalsePChart.Title = "False Positive";
        FalsePChart.ZoomPanBehavior = new ChartZoomPanBehavior()
        {
            EnableDirectionalZooming = true,
            EnableSelectionZooming = true
        };
        FalsePChart.Legend = new ChartLegend()
        {
            IsVisible = true,
            ToggleSeriesVisibility = true

        };
        NumericalAxis primaryAxis = new NumericalAxis();
        FalsePChart.XAxes.Add(primaryAxis);
        NumericalAxis secondaryAxis = new NumericalAxis();
        FalsePChart.YAxes.Add(secondaryAxis);

        foreach (var series in _reportPageModel.AggregatedSeries)
        {
            ScatterSeries scatterSeries = new ScatterSeries()
            {
                Label = $"Scan {series.ScanId}",
                ItemsSource = series.Items,
                XBindingPath = nameof(AggrigatedItems.CweId),
                YBindingPath = nameof(AggrigatedItems.Count),
                PointWidth = 5,
                PointHeight = 5,
                     EnableTooltip = true,
                TooltipTemplate = FalsePToolTip(FalsePChart)
            };
            FalsePChart.Series.Add(scatterSeries);

            // Create an error bar series to display error ranges
            //ErrorBarSeries errorBar = new ErrorBarSeries()
            //{
            //    Label = $"Scan {series.ScanId}",
            //    ItemsSource = series.Items,
            //    XBindingPath = nameof(AggrigatedItems.CWE),
            //    YBindingPath = nameof(AggrigatedItems.Count),
            //    Mode = ErrorBarMode.Vertical,
            //    VerticalErrorPath = nameof(CweTestResults.ErrorValue),
            //    Type = ErrorBarType.Custom
            //};
            //FalsePChart.Series.Add(errorBar);
        }
        // Create a scatter series to plot data points
        //ScatterSeries scatterSeries = new ScatterSeries()
        //{
        //    ItemsSource = _reportPageModel.Items,
        //    XBindingPath = nameof(CweTestResults.TestPathListedCWE),
        //    YBindingPath = nameof(CweTestResults.ScannerFoundCWE),
        //    PointWidth = 5,
        //    PointHeight = 5
        //};




        // Add the both series to the chart's series collection
        //   FalsePChart.Series.Add(scatterSeries);
        // FalsePChart.Series.Add(errorBar);

        return FalsePChart;
    }
    private SfPolarChart BuildPolarChart()
    {

        SfPolarChart FalsePChart = new SfPolarChart();

        FalsePChart.Legend = new ChartLegend()
        {
            IsVisible = true,
            ToggleSeriesVisibility = true

        };
        NumericalAxis primaryAxis = new NumericalAxis();
        FalsePChart.PrimaryAxis = primaryAxis;

        NumericalAxis secondaryAxis = new NumericalAxis();
        FalsePChart.SecondaryAxis = secondaryAxis;
        foreach (var series in _reportPageModel.AggregatedSeries)
        {
            PolarAreaSeries scatterSeries = new PolarAreaSeries()
            {
                Label = $"Scan {series.ScanId}",
                ItemsSource = series.Items,
                XBindingPath = nameof(AggrigatedItems.CweId),
                YBindingPath = nameof(AggrigatedItems.Count),
                ShowDataLabels = true
            };
            FalsePChart.Series.Add(scatterSeries);

            // Create an error bar series to display error ranges
            //ErrorBarSeries errorBar = new ErrorBarSeries()
            //{
            //    Label = $"Scan {series.ScanId}",
            //    ItemsSource = series.Items,
            //    XBindingPath = nameof(CweTestResults.TestPathListedCWE),
            //    YBindingPath = nameof(CweTestResults.ScannerFoundCWE),
            //    Mode = ErrorBarMode.Vertical,
            //    VerticalErrorPath = nameof(CweTestResults.ErrorValue),
            //    Type = ErrorBarType.Custom             
            //};
            // FalsePChart.Series.Add(errorBar);
        }
        // Create a scatter series to plot data points
        //ScatterSeries scatterSeries = new ScatterSeries()
        //{
        //    ItemsSource = _reportPageModel.Items,
        //    XBindingPath = nameof(CweTestResults.TestPathListedCWE),
        //    YBindingPath = nameof(CweTestResults.ScannerFoundCWE),
        //    PointWidth = 5,
        //    PointHeight = 5
        //};




        // Add the both series to the chart's series collection
        //   FalsePChart.Series.Add(scatterSeries);
        // FalsePChart.Series.Add(errorBar);

        return FalsePChart;
    }

    private SfCartesianChart BuildRelatedChart()
    {
       _relatedChart = new SfCartesianChart();

        _relatedChart.Title = "Related Findings";

        _relatedChart.ZoomPanBehavior = new ChartZoomPanBehavior()
        {
            EnableDirectionalZooming = true,
            EnableSelectionZooming = true
        };

        _relatedChart.Legend = new ChartLegend()
        {
            IsVisible = true,
            ToggleSeriesVisibility = true
        };

        NumericalAxis primaryAxis = new();
        _relatedChart.XAxes.Add(primaryAxis);

        NumericalAxis secondaryAxis = new();
        _relatedChart.YAxes.Add(secondaryAxis);

        PopulateRelatedChart();

        return _relatedChart;
    }
    private void PopulateRelatedChart()
    {
        if (_relatedChart == null)
            return;

        _relatedChart.Series.Clear();

        foreach (var series in _reportPageModel.RelatedSeries)
        {
            var filteredItems =
                _reportPageModel.SelectedRelationship == "All"
                    ? series.Items
                    : series.Items
                        .Where(x => x.Relationship ==
                                    _reportPageModel.SelectedRelationship)
                        .ToList();

            if (!filteredItems.Any())
                continue;

            BubbleSeries bubbleSeries = new()
            {
                Label = $"Scan {series.ScanId}",
                ItemsSource = filteredItems,
                XBindingPath = nameof(RelatedItemsInTest.CweId),
                YBindingPath = nameof(RelatedItemsInTest.RelatedId),
                SizeValuePath = nameof(RelatedItemsInTest.Count),
                EnableTooltip = true,
                TooltipTemplate = RelatedToolTip(_relatedChart)
            };

            _relatedChart.Series.Add(bubbleSeries);
        }
    }
    private DataTemplate RelatedToolTip(SfCartesianChart cartesianChart)
    {

        var dataTemplate = new DataTemplate(() =>

         {

             VerticalStackLayout mainlayout = new VerticalStackLayout();

             mainlayout.BackgroundColor = Colors.Black;

             //cwe
             HorizontalStackLayout cweLayout = new HorizontalStackLayout();

             cweLayout.BackgroundColor = Colors.Black;

             Label cweLabel = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White, Text = "CWE:" };

             Label cwe = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White };

             cwe.SetBinding(Label.TextProperty, "Item.CweId");

             cweLayout.Add(cweLabel);

             cweLayout.Add(cwe);
             //cwe
             //relatedcwe
             HorizontalStackLayout relatedcweLayout = new HorizontalStackLayout();

             relatedcweLayout.BackgroundColor = Colors.Black;

             Label relatedcweLabel = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White, Text = "Scanner Identified Related CWE:" };

             Label relatedcwe = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White };

             relatedcwe.SetBinding(Label.TextProperty, "Item.RelatedId");

             relatedcweLayout.Add(relatedcweLabel);

             relatedcweLayout.Add(relatedcwe);
             //relatedcwe

             //Group 1
             HorizontalStackLayout relationshipLayout = new HorizontalStackLayout();

             relationshipLayout.BackgroundColor = Colors.Black;



             Label relationshipLabel = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White, Text = "Relationship:" };

             Label relationship = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White };

             relationship.SetBinding(Label.TextProperty, "Item.Relationship");

             relationshipLayout.Add(relationshipLabel);

             relationshipLayout.Add(relationship);
             //group2
             HorizontalStackLayout relationshipscoreLayout = new HorizontalStackLayout();

             relationshipscoreLayout.BackgroundColor = Colors.Black;

             Label relatedcweScoreLabel = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White, Text = "Scanner Finding Confidence:" };


             Label relationshipscore = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White };

             relationshipscore.SetBinding(Label.TextProperty, "Item.RelationshipScore");

             relationshipscoreLayout.Add(relatedcweScoreLabel);
             relationshipscoreLayout.Add(relationshipscore);

             //group1



             HorizontalStackLayout countLayout = new HorizontalStackLayout();

             countLayout.BackgroundColor = Colors.Black;



             Label countLabel = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White, Text = "Count:" };

             Label count = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White };

             count.SetBinding(Label.TextProperty, "Item.Count", stringFormat: "{0}");

             countLayout.Add(countLabel);

             countLayout.Add(count);



             mainlayout.Add(cweLayout);
             mainlayout.Add(relatedcweLayout);

             mainlayout.Add(relationshipLayout);
             mainlayout.Add(relationshipscoreLayout);

             mainlayout.Add(countLayout);



             return mainlayout;

         });
        return dataTemplate;

    }
    private DataTemplate FalsePToolTip(SfCartesianChart cartesianChart)
    {

        var dataTemplate = new DataTemplate(() =>

        {

            VerticalStackLayout mainlayout = new VerticalStackLayout();

            mainlayout.BackgroundColor = Colors.Black;

            //cwe
            HorizontalStackLayout cweLayout = new HorizontalStackLayout();

            cweLayout.BackgroundColor = Colors.Black;

            Label cweLabel = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White, Text = "CWE:" };

            Label cwe = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White };

            cwe.SetBinding(Label.TextProperty, "Item.CweId");

            cweLayout.Add(cweLabel);

            cweLayout.Add(cwe);
        
       
          

            HorizontalStackLayout countLayout = new HorizontalStackLayout();

            countLayout.BackgroundColor = Colors.Black;



            Label countLabel = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White, Text = "Count:" };

            Label count = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White };

            count.SetBinding(Label.TextProperty, "Item.Count", stringFormat: "{0}");

            countLayout.Add(countLabel);

            countLayout.Add(count);



            mainlayout.Add(cweLayout);

            mainlayout.Add(countLayout);



            return mainlayout;

        });
        return dataTemplate;

    }
    public class ChartData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double HorizontalErrorValue { get; set; }
        public double VerticalErrorValue { get; set; }
    }

}