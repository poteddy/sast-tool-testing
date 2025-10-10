using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Mapping;
using ToolTester.Application.Common.Models;
using ToolTester.Application.CWECatalogs.DTO;

namespace ToolTester.Application.CWECatalogs.Queries
{
   

    public record GetCweCatalogsWithPaginationQuery : IRequest<PaginatedList<CWECatalogDTO>>
    {
        public int Id { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }

    public class GetCweCatalogsWithPaginationQueryHandler : IRequestHandler<GetCweCatalogsWithPaginationQuery, PaginatedList<CWECatalogDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetCweCatalogsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<CWECatalogDTO>> Handle(GetCweCatalogsWithPaginationQuery request, CancellationToken cancellationToken)
        {
            return await _context.CWECatalogs
                .OrderBy(x => x.Name)
                .ProjectTo<CWECatalogDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }
    }
}
