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
    public class Relationssship // The "many" side
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
