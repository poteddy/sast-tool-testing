using AutoMapper;
using ToolTester.Application.Common.Mapping;
using ToolTester.Domain.Entities;

namespace ToolTester.Application.Reports.DTO;

public sealed class ReportDto : IMapFrom<Report>
{
    public int Id { get; set; }

    public int ScanId { get; set; }

    public Scan Scan { get; set; } = null!;

    public int ToolId { get; set; }

    public Tool Tool { get; set; } = null!;

    /*
     * These are actual CWE numbers.
     *
     * They are not foreign keys to CWECatalog.Id.
     */
    public int GroundTruthCweId { get; set; }

    /*
     * Nullable because a scanner may detect a finding
     * without reporting a CWE.
     */
    public int? ScannerCweId { get; set; }

    public int? RootCauseCweId { get; set; }

    public bool DetectedWithoutCwe { get; set; }

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

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Report, ReportDto>();
    }
}
