using System.Collections.Generic;

namespace ToolTester.Application.Reports.DTO
{
    public sealed class HeatmapCellDto
    {
        public int ScanId { get; init; }
        public int CweId { get; init; }
        public string? CweName { get; init; }
        public int Opportunities { get; init; }
        public int Detected { get; init; }
        public int FalseNegatives { get; init; }
        public double DetectionRate { get; init; } // 0..100
    }

    public sealed class ScanInfoDto
    {
        public int ScanId { get; init; }
        public string ScanName { get; init; } = string.Empty;
    }

    public sealed class CweInfoDto
    {
        public int CweId { get; init; }
        public string CweName { get; init; } = string.Empty;
    }

    public sealed class HeatmapDto
    {
        public IReadOnlyList<HeatmapCellDto> Cells { get; init; } = new List<HeatmapCellDto>();
        public IReadOnlyList<ScanInfoDto> Scans { get; init; } = new List<ScanInfoDto>();
        public IReadOnlyList<CweInfoDto> Cwes { get; init; } = new List<CweInfoDto>();
    }
}