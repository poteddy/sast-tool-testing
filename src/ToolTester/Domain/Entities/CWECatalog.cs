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
}
