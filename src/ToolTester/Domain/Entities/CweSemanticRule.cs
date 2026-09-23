using ToolTester.Domain.Common;

namespace ToolTester.Domain.Entities
{
    public sealed class CweSemanticRule:IEntity
    {
        public int Id { get; set; }

        public int SourceCweId { get; set; }

        public int TargetCweId { get; set; }

        public required string Relationship { get; set; }

        public int Score { get; set; }

        public required string Rationale { get; set; }

        public required string EvidenceReference { get; set; }

        public string? ScannerRuleId { get; set; }

        public string? ProgrammingLanguage { get; set; }

        public bool Bidirectional { get; set; }

        public bool Enabled { get; set; } = true;

        public int Version { get; set; } = 1;
    }
}
