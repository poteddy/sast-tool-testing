using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Mapping;
using ToolTester.Application.Common.Models;
using ToolTester.Application.CWETestResultBases.DTO;

namespace ToolTester.Application.CWETestResultBases.Queries
{
    
    public record GetCweTestResultBasesWithPaginationQuery : IRequest<PaginatedList<CweTestResultBaseDTO>>
    {
        public int Id { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }

    public class GetCweTestResultBasesWithPaginationQueryHandler : IRequestHandler<GetCweTestResultBasesWithPaginationQuery, PaginatedList<CweTestResultBaseDTO>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GetCweTestResultBasesWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedList<CweTestResultBaseDTO>> Handle(GetCweTestResultBasesWithPaginationQuery request, CancellationToken cancellationToken)
        {
            return await _context.CWETestResults
                .OrderBy(x => x.TestPathListedCWE)
                .ProjectTo<CweTestResultBaseDTO>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);
        }
    }
}
