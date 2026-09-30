using System;
using System.Collections.Generic;
using System.Text;

namespace ToolTester.Presentation.Models
{
    public class RelationshipMix
    {
        public string Scanner { get; set; } = "";
        public double Exact { get; set; }
        public double DirectSibling { get; set; }
        public double SameRootCauseBroaderCwe { get; set; }
        public double DirectParent { get; set; }
        public double DirectChild { get; set; }
        public double SharedAncestor { get; set; }
        public double CanPrecede { get; set; }
        public double Unrelated { get; set; }
    }
}
