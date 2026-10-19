using System;
using System.Collections.Generic;
using System.Text;
using ToolTester.Domain.Common;

namespace ToolTester.Domain.Entities
{
    public sealed class ToolDeclaredCwe : IEntity
    {
        public int Id { get; set; }

        // FK to Tool
        public int ToolId { get; set; }
        public Tool Tool { get; set; } = null!;

        // CWE number from CWECatalog.CweId (configured as principal key)
        public int CweId { get; set; }
    }
}
