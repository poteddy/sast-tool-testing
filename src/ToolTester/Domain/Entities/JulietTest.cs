using System;
using System.Collections.Generic;
using System.Text;

namespace ToolTester.Domain.Entities
{
    public class JulietTest
    {
        public int Id { get; set; }

        public string NormalizedPath { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public int CweId { get; set; }

        public ICollection<CWETestResult> Results { get; set; } = [];
    }
}
