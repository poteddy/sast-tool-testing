using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolTester.Application.Common.Mapping;
using ToolTester.Application.CWECatalogs.DTO;
using ToolTester.Domain.Entities;

namespace ToolTester.Application.CWETestResultBases.DTO
{
    public class CweTestResultBaseDTO : IMapFrom<CWETestResultBase>
    {
        public void Mapping(Profile profile)
        {
            profile.CreateMap<CWECatalogDTO, CWETestResultBase>()
                 .ForAllMembers(opt => opt.IgnoreSourceWhenDefault());



            profile.CreateMap<CWETestResultBase, CweTestResultBaseDTO>()
                 .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        }

        public int Id { get; set; }
        public int TestPathListedCWE { get; set; }
        public int TestId { get; set; } = 0;
        public int ScannerFoundCWE { get; set; }
    }
}
