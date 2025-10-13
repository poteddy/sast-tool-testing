using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Application.Common.Mapping;
using ToolTester.Domain.Entities;

namespace ToolTester.Application.Reports.DTO
{
    public class ReportDTO : IMapFrom<Report>
    {
        public void Mapping(Profile profile)
        {
            profile.CreateMap<ReportDTO, Report>()
                 .ForAllMembers(opt => opt.IgnoreSourceWhenDefault());



            profile.CreateMap<Report, ReportDTO>()
                 .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        }
        public int Id { get; set; }
        public int CweId { get; set; }
        public int RelatedId { get; set; }

        public int ToolId { get; set; }
        public int ScanId { get; set; }

        public int Count { get; set; }
    }
}
