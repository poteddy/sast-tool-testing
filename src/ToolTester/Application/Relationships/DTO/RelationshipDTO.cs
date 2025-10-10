
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Application.Common.Mapping;
using ToolTester.Domain.Entities;

namespace ToolTester.Application.Relationships.DTO
{
    public class RelationshipDTO : IMapFrom<Relationship>
    {
        public void Mapping(Profile profile)
        {
            profile.CreateMap<RelationshipDTO, Relationship>()
                 .ForAllMembers(opt => opt.IgnoreSourceWhenDefault());



            profile.CreateMap<Relationship, RelationshipDTO>()
                 .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        }
        public int Id { get; set; }

        public int CWEID { get; set; }

        public int RelatedCweID { get; set; }

        public string Nature { get; set; }
        public string Oridinal { get; set; }

        public bool OrderSpecified { get; set; }

        public string? ChainId { get; set; }
   

    }
}
