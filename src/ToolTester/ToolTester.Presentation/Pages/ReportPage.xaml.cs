using MediatR;
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
    public SfCartesianChart BuildFalsePChart()
    {

        SfCartesianChart FalsePChart = new SfCartesianChart();
        FalsePChart.ZoomPanBehavior = new ChartZoomPanBehavior()
        {
            EnableDirectionalZooming = true,
            EnableSelectionZooming = true
        };

        NumericalAxis primaryAxis = new NumericalAxis();
        FalsePChart.XAxes.Add(primaryAxis);
        NumericalAxis secondaryAxis = new NumericalAxis();
        FalsePChart.YAxes.Add(secondaryAxis);

        // Create a scatter series to plot data points
        ScatterSeries scatterSeries = new ScatterSeries()
        {
            ItemsSource = _reportPageModel.Items,
            XBindingPath = nameof(CweTestResults.TestPathListedCWE),
            YBindingPath = nameof(CweTestResults.ScannerFoundCWE),
            PointWidth = 5,
            PointHeight = 5
        };


        // Create an error bar series to display error ranges
        ErrorBarSeries errorBar = new ErrorBarSeries()
        {
            ItemsSource = _reportPageModel.Items,
            XBindingPath = nameof(CweTestResults.TestPathListedCWE),
            YBindingPath = nameof(CweTestResults.ScannerFoundCWE),
            Mode = ErrorBarMode.Vertical,
            VerticalErrorPath = nameof(CweTestResults.ErrorValue),
            Type = ErrorBarType.Custom
        };

        // Add the both series to the chart's series collection
        FalsePChart.Series.Add(scatterSeries);
        FalsePChart.Series.Add(errorBar);

        return FalsePChart;
    }
    public SfCartesianChart BuildRelatedChart()
    {
        
        SfCartesianChart chart = new SfCartesianChart();
        chart.ZoomPanBehavior = new ChartZoomPanBehavior()
        {
            EnableDirectionalZooming = true,
            EnableSelectionZooming = true
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


     

        // Add the both series to the chart's series collection
        chart.Series.Add(scatterSeries);
    

        return chart;
    }
    public class ChartData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double HorizontalErrorValue { get; set; }
        public double VerticalErrorValue { get; set; }
    }

}