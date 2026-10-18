using AutoMapper;
using ToolTester.Application.Common.Mapping;
using ToolTester.Domain.Entities;

namespace ToolTester.Application.JulietCoeverages.DTO
{
    public class JulietCoverageDTO : IMapFrom<JulietCoverage>
    {
        public void Mapping(Profile profile)
        {
            profile.CreateMap<JulietCoverageDTO, JulietCoverage>()
                 .ForAllMembers(opt => opt.IgnoreSourceWhenDefault());



            profile.CreateMap<JulietCoverage, JulietCoverageDTO>()
                 .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        }
        public int Id { get; set; }
        public int CweId { get; set; }
        public int Covered { get; set; }
    }
}
