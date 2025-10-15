namespace ToolTester.Domain.Entities
{
    public class CWETestResultBase
    {
        public int Id { get; set; }
        public int TestPathListedCWE { get; set; }
        public int ScanId { get; set; }
        public int ScannerFoundCWE { get; set; }
    }
}
