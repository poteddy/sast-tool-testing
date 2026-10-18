using AutoMapper;
using ToolTester.Application.Common.Mapping;
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
