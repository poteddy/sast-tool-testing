using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Mapping;
using ToolTester.Application.Common.Models;
using ToolTester.Application.Reports.DTO;

namespace ToolTester.Application.Reports.Quiries
{


    public record GetReportsWithPaginationQuery : IRequest<PaginatedList<ReportDTO>>
    {
        public int Id { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }

    public class GetReportsWithPaginationQueryHandler : IRequestHandler<GetReportsWithPaginationQuery, PaginatedList<ReportDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetReportsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<ReportDTO>> Handle(GetReportsWithPaginationQuery request, CancellationToken cancellationToken)
        {
            return await _context.Reports
                .OrderBy(x => x.CweId)
                .ProjectTo<ReportDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }
    }

}
