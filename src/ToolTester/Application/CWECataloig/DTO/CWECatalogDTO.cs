using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Application.Common.Mapping;
using ToolTester.Domain.Entities;

namespace ToolTester.Application.CWECatalog.DTO
{
    public class CWECatalogDTO:IMapFrom<Domain.Entities.CWECatalog>
    {
        public void Mapping(Profile profile)
        {
            profile.CreateMap<CWECatalogDTO, Domain.Entities.CWECatalog>()
                 .ForAllMembers(opt => opt.IgnoreSourceWhenDefault());



            profile.CreateMap<Domain.Entities.CWECatalog, CWECatalogDTO>()
                 .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        }
        public int Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public string Abstraction { get; set; }

        public string Status { get; set; }

    }
}
