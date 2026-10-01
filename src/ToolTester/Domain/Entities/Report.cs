using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Domain.Common;

namespace ToolTester.Domain.Entities;

public sealed class Report : IEntity
{
    public int Id { get; set; }

    public int ScanId { get; set; }

    public Scan Scan { get; set; } = null!;

    public int ToolId { get; set; }

    public Tool Tool { get; set; } = null!;

    /*
     * These are actual CWE numbers, not foreign keys to
     * CWECatalog.Id.
     */
    public int GroundTruthCweId { get; set; }

    public int ScannerCweId { get; set; }

    public int? RootCauseCweId { get; set; }

    public bool RootCauseMatchesGroundTruth { get; set; }

    public bool RootCauseMatchesScanner { get; set; }

    public int Count { get; set; }

    public string Relationship { get; set; } =
        string.Empty;

    public int RelationshipScore { get; set; }

    public string TopologyRelationship { get; set; } =
        string.Empty;

    public string GroundTruthAbstraction { get; set; } =
        string.Empty;

    public string ScannerAbstraction { get; set; } =
        string.Empty;

    public int? TopologyDistance { get; set; }
}
