namespace ToolTester.Domain.Entities
{
    public class JulietCoverage()
    {
        public int Id { get; set; }
        public int CweId { get; set; }
        public CWECatalog CWECatalog { get; set; }
        public int Covered {  get; set; }
    }
}
