using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Application.Common.Mapping;
using ToolTester.Application.Reports.DTO;
using ToolTester.Domain.Entities;

namespace ToolTester.Application.Tools.DTO
{
    public class ToolDTO : IMapFrom<Tool>
    {
        public void Mapping(Profile profile)
        {
            profile.CreateMap<ToolDTO, Tool>()
                 .ForAllMembers(opt => opt.IgnoreSourceWhenDefault());



            profile.CreateMap<Tool, ToolDTO>()
                 .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Format { get; set; }

    }
}
