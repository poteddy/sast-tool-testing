namespace ToolTester.Domain.Entities
{
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
}
