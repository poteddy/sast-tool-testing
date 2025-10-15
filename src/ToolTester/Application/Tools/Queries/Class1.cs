using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Mapping;
using ToolTester.Application.Common.Models;
using ToolTester.Application.Tools.DTO;


namespace ToolTester.Application.Tools.Queries
{
      public record GetToolsWithPaginationQuery : IRequest<PaginatedList<ToolDTO>>
    {
        public int Id { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }

    public class GetToolsWithPaginationQueryHandler : IRequestHandler<GetToolsWithPaginationQuery, PaginatedList<ToolDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetToolsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<ToolDTO>> Handle(GetToolsWithPaginationQuery request, CancellationToken cancellationToken)
        {
            return await _context.Tools
                .OrderBy(x => x.Id)
                .ProjectTo<ToolDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }
    }

}
