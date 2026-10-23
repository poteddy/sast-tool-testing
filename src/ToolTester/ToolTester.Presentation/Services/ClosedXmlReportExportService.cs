using ClosedXML.Excel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.SKCharts;
using ToolTester.Presentation.PageModels;

namespace ToolTester.Presentation.Services;

/// <summary>
/// Creates an Excel workbook from the data already loaded by ReportPageModel.
/// ClosedXML writes and formats the workbook. LiveCharts2 renders the same
/// series/axes used by the MAUI page to PNG images, which are embedded in Excel.
/// This avoids maintaining a second set of chart calculations.
/// </summary>
public interface IClosedXmlReportExportService
{
    Task ExportAsync(
        ReportPageModel report,
        string outputPath,
        CancellationToken cancellationToken = default);
}

public sealed class ClosedXmlReportExportService : IClosedXmlReportExportService
{
    private const int ChartWidth = 1400;
    private const int StandardChartHeight = 700;
    private const int LargeChartHeight = 900;

    private static readonly XLColor HeaderBlue = XLColor.FromHtml("#1F4E78");
    private static readonly XLColor SectionBlue = XLColor.FromHtml("#D9EAF7");
    private static readonly XLColor WarningFill = XLColor.FromHtml("#FFF2CC");

    public async Task ExportAsync(
        ReportPageModel report,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        if (!report.HasLoadedData)
        {
            throw new InvalidOperationException(
                "ReportPageModel must be loaded before it can be exported.");
        }

        var fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(
            Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The output path has no directory."));

        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "ToolTesterReport-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            // LiveCharts/Skia rendering is CPU-bound. Keep workbook and chart
            // generation away from the MAUI UI thread.
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var workbook = new XLWorkbook();
                workbook.Properties.Title = "ToolTester SAST Benchmark Report";
                workbook.Properties.Subject = "Juliet C/C++ scanner analysis";
                workbook.Properties.Author = "ToolTester";
                workbook.Properties.Category = "Security testing";

                AddSummarySheet(workbook, report);
                AddFalsePositiveSheet(workbook, report, tempDirectory);
                AddUnrelatedConsensusSheet(workbook, report, tempDirectory);
                AddRelatedFindingsSheet(workbook, report, tempDirectory);
                AddHeatmapSheet(workbook, report, tempDirectory);
                AddParetoSheets(workbook, report, tempDirectory);
                AddRelationshipMixSheet(workbook, report, tempDirectory);
                AddScannerDistributionSheet(workbook, report, tempDirectory);
                AddMitreTop25Sheet(workbook, report, tempDirectory);

                cancellationToken.ThrowIfCancellationRequested();
                workbook.SaveAs(fullPath);
            }, cancellationToken);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static void AddSummarySheet(XLWorkbook workbook, ReportPageModel report)
    {
        var ws = workbook.Worksheets.Add("Summary");
        WriteTitle(ws, "ToolTester SAST Benchmark Report");

        ws.Cell("A3").Value = "Generated";
        ws.Cell("B3").Value = DateTimeOffset.Now.LocalDateTime;
        ws.Cell("B3").Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";

        ws.Cell("A4").Value = "High-volume noise excluded";
        ws.Cell("B4").Value = report.ExcludeHighVolumeNoise ? "Yes" : "No";
        ws.Cell("A5").Value = "Noise threshold";
        ws.Cell("B5").Value = "> 10,000 unrelated findings per ScanId + Scanner CWE";
        ws.Cell("A6").Value = "Unrelated consensus groups";
        ws.Cell("B6").Value = report.UnrelatedConsensusGroupCount;
        ws.Cell("A7").Value = "Unrelated consensus findings";
        ws.Cell("B7").Value = report.UnrelatedConsensusFindingCount;

        ws.Cell("A9").Value = "Workbook contents";
        StyleSectionHeader(ws.Range("A9:B9"));

        var sections = new[]
        {
            ("False Positives", "All scanner/CWE false-positive counts; intentionally unfiltered."),
            ("Unrelated Consensus", "Unrelated CWE pairs reported by two or more scanners."),
            ("Related Findings", "Ground-truth CWE versus scanner CWE relationship data."),
            ("Detection Heatmap", "Juliet detection percentage by scanner and ground-truth CWE."),
            ("FN Pareto - Ground Truth", "False negatives ranked by Juliet ground-truth CWE."),
            ("FN Pareto - Declared CWE", "False negatives ranked by scanner-declared CWE."),
            ("Relationship Mix", "Filtered percentages and raw relationship counts."),
            ("Scanner Distribution", "Scanner CWE finding distribution."),
            ("MITRE Top 25", "2025 MITRE CWE Top 25 Juliet C/C++ detection rate.")
        };

        var row = 10;
        foreach (var (name, description) in sections)
        {
            ws.Cell(row, 1).Value = name;
            ws.Cell(row, 2).Value = description;
            row++;
        }

        ws.Range("A3:A7").Style.Font.Bold = true;
        ws.Column(1).Width = 31;
        ws.Column(2).Width = 90;
        ws.SheetView.FreezeRows(2);
        ApplyPageSetup(ws, landscape: false);
    }

    private static void AddFalsePositiveSheet(
        XLWorkbook workbook,
        ReportPageModel report,
        string tempDirectory)
    {
        var ws = workbook.Worksheets.Add("False Positives");
        WriteTitle(ws, "False Positives");
        WriteNote(ws, 3,
            "This view intentionally includes all scanner CWE counts, including results above 10,000 findings.");

        InsertCartesianChart(
            ws, report.FalsePositiveSeries, report.FalsePositiveXAxes,
            report.FalsePositiveYAxes, tempDirectory, "false-positives.png", "A6");
        ApplyPageSetup(ws);
    }

    private static void AddUnrelatedConsensusSheet(
        XLWorkbook workbook,
        ReportPageModel report,
        string tempDirectory)
    {
        var ws = workbook.Worksheets.Add("Unrelated Consensus");
        WriteTitle(ws, "Scanner Consensus - Unrelated Findings");
        ws.Cell("A3").Value = "Consensus groups";
        ws.Cell("B3").Value = report.UnrelatedConsensusGroupCount;
        ws.Cell("D3").Value = "Unrelated findings";
        ws.Cell("E3").Value = report.UnrelatedConsensusFindingCount;
        ws.Range("A3:E3").Style.Font.Bold = true;

        var row = 6;
        WriteHeaders(ws, row,
            "Ground Truth CWE", "Scanner CWE", "Scanner Count",
            "Scan Count", "Finding Count", "Scanners");
        row++;

        foreach (var item in report.UnrelatedConsensusItems)
        {
            ws.Cell(row, 1).Value = $"CWE-{item.GroundTruthCweId}";
            ws.Cell(row, 2).Value = $"CWE-{item.ScannerFoundCweId}";
            ws.Cell(row, 3).Value = item.ScannerCount;
            ws.Cell(row, 4).Value = item.ScanCount;
            ws.Cell(row, 5).Value = item.FindingCount;
            ws.Cell(row, 6).Value = item.ScannerNamesLabel ?? string.Empty;
            row++;
        }

        MakeTable(ws, 6, row - 1, 6, "UnrelatedConsensusTable");
        InsertCartesianChart(
            ws, report.UnrelatedConsensusSeries, report.UnrelatedConsensusXAxes,
            report.UnrelatedConsensusYAxes, tempDirectory, "unrelated-consensus.png", "H3",
            LargeChartHeight);
        FinishDataSheet(ws, 6);
    }

    private static void AddRelatedFindingsSheet(
        XLWorkbook workbook,
        ReportPageModel report,
        string tempDirectory)
    {
        var ws = workbook.Worksheets.Add("Related Findings");
        WriteTitle(ws, "Related Findings");
        WriteNote(ws, 3,
            $"Relationship filter at export: {report.SelectedRelationship}. " +
            $"High-volume noise exclusion: {(report.ExcludeHighVolumeNoise ? "enabled" : "disabled")}.");

        var row = 6;
        WriteHeaders(ws, row, "Scan ID", "Scan Name", "Ground Truth CWE",
            "Scanner CWE", "Relationship", "Confidence", "Count");
        row++;

        foreach (var series in report.RelatedSeries.OrderBy(x => x.ScanId))
        {
            foreach (var item in series.Items.OrderBy(x => x.GroundTruthCweId))
            {
                ws.Cell(row, 1).Value = series.ScanId;
                ws.Cell(row, 2).Value = $"Scan {series.ScanId}";
                ws.Cell(row, 3).Value = $"CWE-{item.GroundTruthCweId}";
                ws.Cell(row, 4).Value = item.ScannerCweId.HasValue
                    ? $"CWE-{item.ScannerCweId.Value}"
                    : string.Empty;
                ws.Cell(row, 5).Value = item.Relationship ?? string.Empty;
                ws.Cell(row, 6).Value = item.RelationshipScore;
                ws.Cell(row, 7).Value = item.Count;
                row++;
            }
        }

        MakeTable(ws, 6, row - 1, 7, "RelatedFindingsTable");
        InsertCartesianChart(
            ws, report.RelatedFindingSeries, report.RelatedFindingXAxes,
            report.RelatedFindingYAxes, tempDirectory, "related-findings.png", "I3");
        FinishDataSheet(ws, 7);
    }

    private static void AddHeatmapSheet(
        XLWorkbook workbook,
        ReportPageModel report,
        string tempDirectory)
    {
        var ws = workbook.Worksheets.Add("Detection Heatmap");
        WriteTitle(ws, "Juliet CWE Detection Heatmap By Scanner");
        WriteNote(ws, 3,
            "Each cell shows the percentage of known Juliet tests detected by a scanner for a ground-truth CWE.");

        if (report.HasScannerCweHeatmapData)
        {
            InsertCartesianChart(
                ws, report.ScannerCweHeatmapSeries, report.ScannerCweHeatmapXAxes,
                report.ScannerCweHeatmapYAxes, tempDirectory, "detection-heatmap.png", "A6",
                LargeChartHeight);
        }
        else
        {
            ws.Cell("A6").Value = "No heatmap data was available at export time.";
        }

        ApplyPageSetup(ws);
    }

    private static void AddParetoSheets(
        XLWorkbook workbook,
        ReportPageModel report,
        string tempDirectory)
    {
        AddParetoSheet(
            workbook, "FN Pareto - Ground Truth", "False Negative Pareto - Ground Truth",
            report.GroundTruthParetoCharts, tempDirectory, "ground-truth");
        AddParetoSheet(
            workbook, "FN Pareto - Declared CWE", "False Negative Pareto - Declared CWE",
            report.DeclaredCweParetoCharts, tempDirectory, "declared-cwe");
    }

    private static void AddParetoSheet(
        XLWorkbook workbook,
        string sheetName,
        string title,
        IEnumerable<ToolTester.Presentation.Models.Charts.ParetoChartModel> charts,
        string tempDirectory,
        string filePrefix)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        WriteTitle(ws, title);
        var anchorRow = 4;
        var index = 0;

        foreach (var chart in charts)
        {
            ws.Cell(anchorRow, 1).Value = chart.Title;
            ws.Cell(anchorRow, 1).Style.Font.Bold = true;
            ws.Cell(anchorRow, 1).Style.Font.FontSize = 14;

            InsertCartesianChart(
                ws, chart.Series, chart.XAxes, chart.YAxes,
                tempDirectory, $"{filePrefix}-{index++}.png", $"A{anchorRow + 2}");
            anchorRow += 39;
        }

        if (index == 0)
        {
            ws.Cell("A4").Value = "No false-negative Pareto data was available at export time.";
        }

        ApplyPageSetup(ws);
    }

    private static void AddRelationshipMixSheet(
        XLWorkbook workbook,
        ReportPageModel report,
        string tempDirectory)
    {
        var ws = workbook.Worksheets.Add("Relationship Mix");
        WriteTitle(ws, "Relationship Mix By Scanner");
        WriteNote(ws, 3,
            "The percentage chart follows the page's noise filter. The count chart includes raw findings, including high-volume results.");

        var row = 6;
        WriteHeaders(ws, row, "Scan ID", "Scan Name", "Scanner", "Relationship",
            "Count", "Percentage", "Low", "High");
        row++;

        foreach (var item in report.RelationshipMixChartData
                     .OrderBy(x => x.ScanName)
                     .ThenBy(x => x.Relationship))
        {
            ws.Cell(row, 1).Value = item.ScanId;
            ws.Cell(row, 2).Value = item.ScanName ?? string.Empty;
            ws.Cell(row, 3).Value = item.ScannerName ?? string.Empty;
            ws.Cell(row, 4).Value = item.Relationship ?? string.Empty;
            ws.Cell(row, 5).Value = item.Count;
            ws.Cell(row, 6).Value = item.Percentage / 100d;
            ws.Cell(row, 7).Value = item.Low / 100d;
            ws.Cell(row, 8).Value = item.High / 100d;
            row++;
        }

        if (row > 7)
        {
            ws.Range(7, 6, row - 1, 8).Style.NumberFormat.Format = "0.00%";
        }
        MakeTable(ws, 6, row - 1, 8, "RelationshipMixTable");

        InsertCartesianChart(
            ws, report.RelationshipMixChartSeries, report.RelationshipMixXAxes,
            report.RelationshipMixYAxes, tempDirectory, "relationship-mix-percent.png", "J3");
        InsertCartesianChart(
            ws, report.RelationshipMixCountChartSeries, report.RelationshipMixCountXAxes,
            report.RelationshipMixCountYAxes, tempDirectory, "relationship-mix-count.png", "J39");
        FinishDataSheet(ws, 8);
    }

    private static void AddScannerDistributionSheet(
        XLWorkbook workbook,
        ReportPageModel report,
        string tempDirectory)
    {
        var ws = workbook.Worksheets.Add("Scanner Distribution");
        WriteTitle(ws, "Scanner CWE Distribution");
        WriteNote(ws, 3,
            $"High-volume noise exclusion: {(report.ExcludeHighVolumeNoise ? "enabled" : "disabled")}.");

        InsertPolarChart(
            ws, report.PolarSeries, report.PolarAngleAxes, report.PolarRadiusAxes,
            tempDirectory, "scanner-distribution.png", "A6");
        ApplyPageSetup(ws);
    }

    private static void AddMitreTop25Sheet(
        XLWorkbook workbook,
        ReportPageModel report,
        string tempDirectory)
    {
        var ws = workbook.Worksheets.Add("MITRE Top 25");
        WriteTitle(ws, "2025 MITRE CWE Top 25 - Juliet C/C++ Detection Rate");
        WriteNote(ws, 3,
            "Each line represents a scanner. Missing points mean the C/C++ benchmark has no opportunity for that CWE.");

        if (report.HasMitreTop25Data)
        {
            InsertCartesianChart(
                ws, report.MitreTop25Series, report.MitreTop25XAxes,
                report.MitreTop25YAxes, tempDirectory, "mitre-top-25.png", "A6");
        }
        else
        {
            ws.Cell("A6").Value = "No MITRE Top 25 data was available at export time.";
        }

        ApplyPageSetup(ws);
    }

    private static void InsertCartesianChart(
        IXLWorksheet worksheet,
        ISeries[] series,
        Axis[] xAxes,
        Axis[] yAxes,
        string tempDirectory,
        string fileName,
        string anchorCell,
        int height = StandardChartHeight)
    {
        if (series.Length == 0)
        {
            worksheet.Cell(anchorCell).Value = "No chart data was available at export time.";
            return;
        }

        var imagePath = Path.Combine(tempDirectory, fileName);
        var chart = new SKCartesianChart
        {
            Width = ChartWidth,
            Height = height,
            Series = series,
            XAxes = xAxes,
            YAxes = yAxes,
            LegendPosition = LiveChartsCore.Measure.LegendPosition.Top,
            Background = SkiaSharp.SKColors.White
        };
        chart.SaveImage(imagePath);

        worksheet.AddPicture(imagePath)
            .MoveTo(worksheet.Cell(anchorCell))
            .WithSize(ChartWidth, height);
    }

    private static void InsertPolarChart(
        IXLWorksheet worksheet,
        ISeries[] series,
        PolarAxis[] angleAxes,
        PolarAxis[] radiusAxes,
        string tempDirectory,
        string fileName,
        string anchorCell)
    {
        if (series.Length == 0)
        {
            worksheet.Cell(anchorCell).Value = "No chart data was available at export time.";
            return;
        }

        var imagePath = Path.Combine(tempDirectory, fileName);
        var chart = new SKPolarChart
        {
            Width = ChartWidth,
            Height = StandardChartHeight,
            Series = series,
            AngleAxes = angleAxes,
            RadiusAxes = radiusAxes,
            LegendPosition = LiveChartsCore.Measure.LegendPosition.Top,
            Background = SkiaSharp.SKColors.White
        };
        chart.SaveImage(imagePath);

        worksheet.AddPicture(imagePath)
            .MoveTo(worksheet.Cell(anchorCell))
            .WithSize(ChartWidth, StandardChartHeight);
    }

    private static void WriteTitle(IXLWorksheet ws, string title)
    {
        ws.Cell("A1").Value = title;
        ws.Range("A1:H1").Merge();
        ws.Range("A1:H1").Style
            .Font.SetBold()
            .Font.SetFontSize(20)
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(HeaderBlue)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        ws.Row(1).Height = 30;
    }

    private static void WriteNote(IXLWorksheet ws, int row, string text)
    {
        ws.Cell(row, 1).Value = text;
        ws.Range(row, 1, row + 1, 8).Merge();
        ws.Range(row, 1, row + 1, 8).Style
            .Fill.SetBackgroundColor(WarningFill)
            .Alignment.SetWrapText()
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        ws.Range(row, 1, row + 1, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Row(row).Height = 22;
        ws.Row(row + 1).Height = 22;
    }

    private static void WriteHeaders(IXLWorksheet ws, int row, params string[] headers)
    {
        for (var column = 0; column < headers.Length; column++)
        {
            ws.Cell(row, column + 1).Value = headers[column];
        }
        StyleSectionHeader(ws.Range(row, 1, row, headers.Length));
    }

    private static void StyleSectionHeader(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = SectionBlue;
        range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
    }

    private static void MakeTable(
        IXLWorksheet ws,
        int headerRow,
        int lastRow,
        int lastColumn,
        string tableName)
    {
        if (lastRow <= headerRow)
        {
            return;
        }

        var table = ws.Range(headerRow, 1, lastRow, lastColumn).CreateTable(tableName);
        table.Theme = XLTableTheme.TableStyleMedium2;
        table.ShowAutoFilter = true;
    }

    private static void FinishDataSheet(IXLWorksheet ws, int lastColumn)
    {
        ws.SheetView.FreezeRows(6);
        for (var column = 1; column <= lastColumn; column++)
        {
            ws.Column(column).AdjustToContents(1, 60);
            if (ws.Column(column).Width > 45)
            {
                ws.Column(column).Width = 45;
            }
        }
        ApplyPageSetup(ws);
    }

    private static void ApplyPageSetup(IXLWorksheet ws, bool landscape = true)
    {
        ws.Style.Font.FontName = "Aptos";
        ws.Style.Font.FontSize = 10;
        ws.PageSetup.PageOrientation = landscape
            ? XLPageOrientation.Landscape
            : XLPageOrientation.Portrait;
        ws.PageSetup.PaperSize = XLPaperSize.LetterPaper;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.Margins.Left = 0.25;
        ws.PageSetup.Margins.Right = 0.25;
        ws.PageSetup.Margins.Top = 0.5;
        ws.PageSetup.Margins.Bottom = 0.5;
        ws.ShowGridLines = false;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // A failed temp cleanup must not invalidate a successfully saved report.
        }
    }
}
