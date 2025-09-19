using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToolTester.Parsers.Sarif.Interfaces
{
    public interface ICWEs
    {
    }
    public class CWEs : ICWEs
    {
        public CWEs(string title, int test, string numericalSeverity, List<int?> foundBy, string severity, string description, bool staticFinding, bool dynamicFinding, string? filePath, int? line, string references)
        {
            Title = title;
            Test = test;
            NumericalSeverity = numericalSeverity;
            FoundBy = foundBy;
            Severity = severity;
            Description = description;
            StaticFinding = staticFinding;
            DynamicFinding = dynamicFinding;
            FilePath = filePath;
            Line = line;
            References = references;
        }

        public string Title { get; }
        public int Test { get; }
        public string NumericalSeverity { get; }
        public List<int?> FoundBy { get; }
        public string Severity { get; }
        public string Description { get; }
        public bool StaticFinding { get; }
        public bool DynamicFinding { get; }
        public string? FilePath { get; }
        public int? Line { get; }
        public string References { get; }
        public string VulnIdFromTool { get; internal set; }
        public string Cve { get; internal set; }
        public int Cwe { get; internal set; }
        public string Mitigation { get; internal set; }
        public DateTime? Date { get; internal set; }
    }
}
