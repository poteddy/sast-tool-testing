namespace ToolTester.Presentation.Models
{
    public class ScanReport
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;

        // Total findings in the scan
        public int FindingsCount { get; set; }

        // Findings per CWE (key = CWE id or name)
        public Dictionary<string, int> FindingsPerCwe { get; set; } = new();

        // Findings per severity label (e.g., "High", "Medium", "Low")
        public Dictionary<string, int> FindingsPerSeverity { get; set; } = new();
    }
}