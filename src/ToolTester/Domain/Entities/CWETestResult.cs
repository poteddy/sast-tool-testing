namespace ToolTester.Domain.Entities
{
    public class CWETestResult:CWETestResultBase
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

       
        public string NumericalSeverity { get; set; } = string.Empty;
        public List<int?> FoundBy { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool StaticFinding { get; set; } = false;
        public bool DynamicFinding { get; set; } = false;
        public string? FilePath { get; set; }
        public int? Line { get; set; }
        public string References { get; set; } = string.Empty;
        public string VulnIdFromTool { get; set; } = string.Empty;
        public string Cve { get; set; } = string.Empty;
      
        public string Mitigation { get; set; } = string.Empty;
        public DateTime? Date { get; set; } = DateTime.MinValue;
            
        public Scan Scan { get; set; }
        public int Test { get; set; }
    }
}
