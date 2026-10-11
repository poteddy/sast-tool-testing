using AutoMapper;
using ToolTester.Application.Common.Mapping;
using ToolTester.Domain.Entities;

namespace ToolTester.Application.CWETestResultBases.DTO;

public class CweTestResultBaseDTO :
    IMapFrom<CWETestResult>
{
    public int Id { get; set; }

    public int ScanId { get; set; }

    public int? ScannerFoundCWE { get; set; }

    public int TestId { get; set; }

    public int GroundTruthCwe { get; set; }

    public string TestFileName { get; set; } =
        string.Empty;

    public string TestPath { get; set; } =
        string.Empty;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<
                CWETestResult,
                CweTestResultBaseDTO>()
            .ForMember(
                destination =>
                    destination.GroundTruthCwe,
                options =>
                    options.MapFrom(
                        source =>
                            source.Test.CweId))
            .ForMember(
                destination =>
                    destination.TestFileName,
                options =>
                    options.MapFrom(
                        source =>
                            source.Test.FileName))
            .ForMember(
                destination =>
                    destination.TestPath,
                options =>
                    options.MapFrom(
                        source =>
                            source.Test.NormalizedPath));
    }
}