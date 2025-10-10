using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Application.Common.Mapping;
using ToolTester.Application.CWECatalogs.DTO;
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
        public int CWE_ID { get; set; }
        public int Covered { get; set; }
    }
}
