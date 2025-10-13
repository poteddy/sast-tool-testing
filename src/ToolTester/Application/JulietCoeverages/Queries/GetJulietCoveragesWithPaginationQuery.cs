using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Mapping;
using ToolTester.Application.Common.Models;
using ToolTester.Application.JulietCoeverages.DTO;

namespace ToolTester.Application.JulietCoeverages.Queries
{

    public record GetJulietCoveragesWithPaginationQuery : IRequest<PaginatedList<JulietCoverageDTO>>
    {
        public int Id { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }

    public class GetJulietCoveragesWithPaginationQueryHandler : IRequestHandler<GetJulietCoveragesWithPaginationQuery, PaginatedList<JulietCoverageDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetJulietCoveragesWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<JulietCoverageDTO>> Handle(GetJulietCoveragesWithPaginationQuery request, CancellationToken cancellationToken)
        {
            return await _context.JulietCoverages
                .OrderBy(x => x.CweId)
                .ProjectTo<JulietCoverageDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }
    }
}
