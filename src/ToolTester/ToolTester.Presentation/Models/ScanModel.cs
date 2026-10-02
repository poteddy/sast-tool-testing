using System;
using System.Collections.Generic;
using System.Text;

namespace ToolTester.Presentation.Models
{
    public class ScanModel
    {
        public int Id { get; set; } // use GUID/string if preferred
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
