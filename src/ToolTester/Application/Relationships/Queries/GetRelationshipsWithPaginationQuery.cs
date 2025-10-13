using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Mapping;
using ToolTester.Application.Common.Models;
using ToolTester.Application.Relationships.DTO;
using ToolTester.Domain.Entities;

namespace ToolTester.Application.Relationships.Queries
    {


        public record GetRelationshipsWithPaginationQuery : IRequest<PaginatedList<RelationshipDTO>>
        {
            public int Id { get; init; }
            public int PageNumber { get; init; } = 1;
            public int PageSize { get; init; } = 10;
        }

        public class GetRelationshipsWithPaginationQueryHandler : IRequestHandler<GetRelationshipsWithPaginationQuery, PaginatedList<RelationshipDTO>>
        {
            private readonly IApplicationDbContext _context;
            private readonly IMapper _mapper;

            public GetRelationshipsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
            {
                _context = context;
                _mapper = mapper;
            }

            public async Task<PaginatedList<RelationshipDTO>> Handle(GetRelationshipsWithPaginationQuery request, CancellationToken cancellationToken)
            {
                return await _context.Relationships
                    .OrderBy(x => x.CweId)
                    .ProjectTo<RelationshipDTO>(_mapper.ConfigurationProvider)
                    .PaginatedListAsync(request.PageNumber, request.PageSize);
            }
        }
    }
