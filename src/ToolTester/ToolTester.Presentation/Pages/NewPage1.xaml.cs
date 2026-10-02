using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace ToolTester.Presentation.Pages;

public partial class NewPage1 : ContentPage
{
    public NewPage1()
    {
        InitializeComponent();
        BindingContext = this;
    }
    public ISeries[] Series { get; set; } = [new ColumnSeries<int> { Values = [10, 20, 15, 30, 25] }];
    protected override void OnNavigatingFrom(NavigatingFromEventArgs e)
    {
        base.OnNavigatingFrom(e);

        // Clear the binding context to stop LiveCharts from listening to data changes
        BindingContext = null;

        // Explicitly suggest a GC pass to clean up native Skia handlers before the Frame shifts
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}