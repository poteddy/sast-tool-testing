namespace ToolTester.Domain.Entities
{
    public sealed class JulietCoverage
    {
        public int Id { get; set; }

        // Actual CWE number, for example 78, 126, or 129.
        public int CweId { get; set; }

        public int Covered { get; set; }

    }
}
