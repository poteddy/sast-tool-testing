namespace ToolTester.Domain.Entities
{

    public class CWETestResultBase
    {
        public int Id { get; set; }      

        public int ScanId { get; set; }

        public int? ScannerFoundCWE { get; set; }

        public int TestId { get; set; }

        public Scan Scan { get; set; } = null!;

        public JulietTest Test { get; set; } = null!;
    }


}
