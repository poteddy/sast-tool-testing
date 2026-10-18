namespace ToolTester.Application.Reports.DTO;

public sealed class FalsePositivePointDto
{
    public int CweId { get; init; }
    public int Count { get; init; }
}

public sealed class ScanFalsePositiveSeriesDto
{
    public int ScanId { get; init; }
    public string ScanName { get; init; } = string.Empty;
    public IReadOnlyList<FalsePositivePointDto> Points { get; init; } = new List<FalsePositivePointDto>();
}

public sealed class FalsePositiveChartDto
{
    public IReadOnlyList<ScanFalsePositiveSeriesDto> Series { get; init; } = new List<ScanFalsePositiveSeriesDto>();
}