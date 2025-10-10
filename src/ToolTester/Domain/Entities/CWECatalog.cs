using System.ComponentModel.DataAnnotations;
using ToolTester.Domain.Coomon;

namespace ToolTester.Domain.Entities
{
    public class CWECatalog : IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public string Abstraction { get; set; }

        public string Status { get; set; }
    
    }
    public class JulietCoverage()
    {
        public int Id { get; set; }
        public int CWE_ID { get; set; }
        public int Covered {  get; set; }
    }
    public class Relationship // The "many" side
    {
        public int Id { get; set; }

        public int CWEID { get; set; }
     
        public int RelatedCweID { get; set; }

        public string Nature { get; set; }
        public string Oridinal { get; set; }

        public bool OrderSpecified { get; set; }

        public string? ChainId { get; set; }
      
    
    }
   
    public class CWETestResultBase
    {
        public int Id { get; set; }
        public int TestPathListedCWE { get; set; }
        public int Test { get; set; } = 0;
        public int ScannerFoundCWE { get; set; }
    }

   
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
    }
}
