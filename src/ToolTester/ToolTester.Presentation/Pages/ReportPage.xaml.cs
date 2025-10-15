using MediatR;
using Microsoft.Maui.Controls;
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

    public ReportPage(ReportPageModel reportPageModel)
    {
        InitializeComponent();
        _reportPageModel = reportPageModel;
        _reportPageModel.LoadItemsAsync().FireAndForgetSafeAsync();
        BindingContext = _reportPageModel;


        StackLayout layout = new StackLayout
        {
            Children =
            {
               BuildFalsePChart(),
               BuildRelatedChart()
               
            }
        };
        this.Content= layout;
   
    }
    private SfCartesianChart BuildFalsePChart()
    {

        SfCartesianChart FalsePChart = new SfCartesianChart();
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

        foreach (var series in _reportPageModel.TestSeries)
        {
            ScatterSeries scatterSeries = new ScatterSeries()
            {
                Label = $"Scan {series.ScanId}",
                ItemsSource = series.Items,
                XBindingPath = nameof(CweTestResults.TestPathListedCWE),
                YBindingPath = nameof(CweTestResults.ScannerFoundCWE),
                PointWidth = 5,
                PointHeight = 5
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
        
        SfCartesianChart chart = new SfCartesianChart();
        chart.ZoomPanBehavior = new ChartZoomPanBehavior()
        {
            EnableDirectionalZooming = true,
            EnableSelectionZooming = true
        };
        chart.Legend = new ChartLegend()
        {
            IsVisible = true,
            ToggleSeriesVisibility = true

        };
    
        NumericalAxis primaryAxis = new NumericalAxis();
        chart.XAxes.Add(primaryAxis);
        NumericalAxis secondaryAxis = new NumericalAxis();
        chart.YAxes.Add(secondaryAxis);

        // Create a scatter series to plot data points
        BubbleSeries scatterSeries = new BubbleSeries()
        {
            ItemsSource = _reportPageModel.ReletedItems,            
            XBindingPath = nameof(RelatedItemsInTest.CweId),
            YBindingPath = nameof(RelatedItemsInTest.RelatedId),
             SizeValuePath = nameof(RelatedItemsInTest.Count)
            

        };


        scatterSeries.EnableTooltip = true;        
        scatterSeries.TooltipTemplate = ToolTip(chart);
        // Add the both series to the chart's series collection
        chart.Series.Add(scatterSeries);
    

        return chart;
    }
    private DataTemplate ToolTip(SfCartesianChart cartesianChart)
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

            Label relatedcweLabel = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White, Text = "Related CWE:" };

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
            //group1




            HorizontalStackLayout countLayout = new HorizontalStackLayout();

            countLayout.BackgroundColor = Colors.Black;



            Label countLabel = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White, Text = "Count:" };

            Label count = new Label() { Padding = 2, FontSize = 10, TextColor = Colors.White };

            count.SetBinding(Label.TextProperty, "Item.Count", stringFormat: "{0}$");

            countLayout.Add(countLabel);

            countLayout.Add(count);



            mainlayout.Add(cweLayout);
            mainlayout.Add(relatedcweLayout);

            mainlayout.Add(relationshipLayout);

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