using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ToolTester.Application.Common.Interfaces;
using ToolTester.Application.Common.Mapping;
using ToolTester.Application.Common.Models;
using ToolTester.Application.CWETestResultBases.DTO;

namespace ToolTester.Application.CWETestResultBases.Queries;

public record GetCweTestResultBasesWithPaginationQuery
    : IRequest<PaginatedList<CweTestResultBaseDTO>>
{
    public int Id { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public class GetCweTestResultBasesWithPaginationQueryHandler
    : IRequestHandler<
        GetCweTestResultBasesWithPaginationQuery,
        PaginatedList<CweTestResultBaseDTO>>
{
    private readonly IApplicationDbContext _context;

    private readonly IMapper _mapper;

    public GetCweTestResultBasesWithPaginationQueryHandler(
        IApplicationDbContext context,
        IMapper mapper)
    {
        _context = context
            ?? throw new ArgumentNullException(
                nameof(context));

        _mapper = mapper
            ?? throw new ArgumentNullException(
                nameof(mapper));
    }

    public async Task<PaginatedList<CweTestResultBaseDTO>> Handle(
        GetCweTestResultBasesWithPaginationQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.CWETestResults
            .AsNoTracking()
            .OrderBy(result => result.Test.CweId)
            .ThenBy(result => result.Test.NormalizedPath)
            .ThenBy(result => result.Id)
            .ProjectTo<CweTestResultBaseDTO>(
                _mapper.ConfigurationProvider)
            .PaginatedListAsync(
                request.PageNumber,
                request.PageSize);
    }
}