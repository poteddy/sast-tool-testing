using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Vml.Spreadsheet;
using LiveChartsCore;

namespace ToolTester.Presentation.Models.Charts;

public partial class ChartLegendItem : ObservableObject
{
    private bool _isUpdatingSeries;

    public ChartLegendItem(
        ISeries series,
        string color,
        bool isVisible = true)
    {
        Series = series ??
            throw new ArgumentNullException(nameof(series));

        Name = string.IsNullOrWhiteSpace(series.Name)
            ? "Unnamed series"
            : series.Name;

        Color = color;

        _isVisible = isVisible;
        Series.IsVisible = isVisible;
    }

    public string Name { get; }

    public string Color { get; }

    public ISeries Series { get; }

    [ObservableProperty]
    private bool _isVisible;

    partial void OnIsVisibleChanged(bool value)
    {
        if (_isUpdatingSeries)
        {
            return;
        }

        Series.IsVisible = value;
    }

    [RelayCommand]
    private void Toggle()
    {
        IsVisible = !IsVisible;
    }

    public void SetVisibility(bool isVisible)
    {
        IsVisible = isVisible;
    }

    public void SynchronizeFromSeries()
    {
        try
        {
            _isUpdatingSeries = true;
            IsVisible = Series.IsVisible;
        }
        finally
        {
            _isUpdatingSeries = false;
        }
    }
}