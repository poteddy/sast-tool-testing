namespace ToolTester.Domain.Entities
{
    public class Relationship // The "many" side
    {
        public int Id { get; set; }

        public int CweId { get; set; }
 
        public int RelatedCweID { get; set; }

        public string Nature { get; set; }
        public string? Ordinal { get; set; }
        public bool OrderSpecified { get; set; }

        public string? ChainId { get; set; }

        public int? ViewId { get; set; }
        public bool IsDerived { get; set; }

        public int Distance { get; set; } = 1;

        public required string Source { get; set; }
    }
}
