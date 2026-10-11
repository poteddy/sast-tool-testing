namespace ToolTester.Domain.Entities
{
    public class CWETestResult : CWETestResultBase
    {
        public string Title { get; set; } = string.Empty;

        public string NumericalSeverity { get; set; } = string.Empty;

        public List<int?> FoundBy { get; set; } = [];

        public string Severity { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool StaticFinding { get; set; }

        public bool DynamicFinding { get; set; }

        // IMPORTANT:
        // This is exactly what the scanner reported.
        // Do not use it as the relational identity of the test.
        public string? FilePath { get; set; }

        public int? Line { get; set; }

        public string References { get; set; } = string.Empty;

        public string VulnIdFromTool { get; set; } = string.Empty;

        public string Cve { get; set; } = string.Empty;

        public string Mitigation { get; set; } = string.Empty;

        public DateTime? Date { get; set; }

        public int? RootCauseCweId { get; set; }
    }
}
