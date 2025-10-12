namespace ToolTester.Domain.Entities
{
    public class CWETestResultBase
    {
        public int Id { get; set; }
        public int TestPathListedCWE { get; set; }
        public int Test { get; set; } = 0;
        public int ScannerFoundCWE { get; set; }
    }
}
