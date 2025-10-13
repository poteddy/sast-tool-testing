using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Domain.Coomon;

namespace ToolTester.Domain.Entities
{
    public class Report: IEntity
    {
        public int Id { get; set; }
        public int CweId {  get; set; }
        public CWECatalog CWECatalog { get; set; }
        public int RelatedId {  get; set; }

        public int ToolId {  get; set; }
        public int ScanId {  get; set; }

        public int Count { get; set; }
    }
}
