
using Microsoft.EntityFrameworkCore;
using ToolTester.Application.CweRelationshipEngine;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services;

public sealed class CweTopologyService : ICweTopologyService
{
    private const int ResearchConceptsViewId = 1000;

    private readonly IDbContextFactory<ApplicationDbContext>
        _contextFactory;

    public CweTopologyService(
        IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory
            ?? throw new ArgumentNullException(
                nameof(contextFactory));
    }

    public async Task<CweTopologyMatch> GetRelationshipAsync(
        int sourceCweId, //this is the test case cwe
        int targetCweId, //this is the scanner reported cwe
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        var topology =
            await LoadTopologyAsync(
                context,
                cancellationToken);

        var sourceFound =
     topology.Nodes.TryGetValue(
         sourceCweId,
         out var source);

        var targetFound =
            topology.Nodes.TryGetValue(
                targetCweId,
                out var target);

        if (!sourceFound && !targetFound)
        {
            return new CweTopologyMatch(
                sourceCweId,
                targetCweId,
                CweAbstractionLevel.Unknown,
                CweAbstractionLevel.Unknown,
                CweTopologyRelationshipKind.BothCweMissing,
                null,
                [],
                []);
        }

        if (!sourceFound)
        {
            return CweTopologyMatch.MissingSource(
                sourceCweId,
                targetCweId,
                target!);
        }

        if (!targetFound)
        {
            return CweTopologyMatch.MissingTarget(
                sourceCweId,
                targetCweId,
                source!);
        }

        if (sourceCweId == targetCweId)
        {
            return new CweTopologyMatch(
                SourceCweId: sourceCweId,
                TargetCweId: targetCweId,
                SourceAbstraction: source.Abstraction,
                TargetAbstraction: target.Abstraction,
                Relationship:
                    CweTopologyRelationshipKind.SameCwe,
                Distance: 0,
                Path: [source],
                CommonAncestors: []);
        }

        var sourceToTargetPath =
            FindAncestorPath(
                sourceCweId,
                targetCweId,
                topology.ParentIdsByChildId);

        if (sourceToTargetPath is not null)
        {
            return new CweTopologyMatch(
                SourceCweId: sourceCweId,
                TargetCweId: targetCweId,
                SourceAbstraction: source.Abstraction,
                TargetAbstraction: target.Abstraction,
                Relationship:
                    CweTopologyRelationshipKind.DescendantOf,
                Distance: sourceToTargetPath.Count - 1,
                Path: ConvertToNodes(
                    sourceToTargetPath,
                    topology.Nodes),
                CommonAncestors: []);
        }

        var targetToSourcePath =
            FindAncestorPath(
                targetCweId,
                sourceCweId,
                topology.ParentIdsByChildId);

        if (targetToSourcePath is not null)
        {
            var sourceToTarget =
                targetToSourcePath
                    .AsEnumerable()
                    .Reverse()
                    .ToArray();

            return new CweTopologyMatch(
                SourceCweId: sourceCweId,
                TargetCweId: targetCweId,
                SourceAbstraction: source.Abstraction,
                TargetAbstraction: target.Abstraction,
                Relationship:
                    CweTopologyRelationshipKind.AncestorOf,
                Distance: sourceToTarget.Count() - 1,
                Path: ConvertToNodes(
                    sourceToTarget,
                    topology.Nodes),
                CommonAncestors: []);
        }

        var sourceAncestors =
            FindAllAncestorDistances(
                sourceCweId,
                topology.ParentIdsByChildId);

        var targetAncestors =
            FindAllAncestorDistances(
                targetCweId,
                topology.ParentIdsByChildId);

        var commonAncestorIds =
            sourceAncestors.Keys
                .Intersect(targetAncestors.Keys)
                .OrderBy(id =>
                    sourceAncestors[id] +
                    targetAncestors[id])
                .ThenBy(id => id)
                .ToArray();

        if (commonAncestorIds.Length > 0)
        {
            return new CweTopologyMatch(
                SourceCweId: sourceCweId,
                TargetCweId: targetCweId,
                SourceAbstraction: source.Abstraction,
                TargetAbstraction: target.Abstraction,
                Relationship:
                    CweTopologyRelationshipKind
                        .SharesAncestorWith,
                Distance: null,
                Path: [],
                CommonAncestors: ConvertToNodes(
                    commonAncestorIds,
                    topology.Nodes));
        }

        return new CweTopologyMatch(
            SourceCweId: sourceCweId,
            TargetCweId: targetCweId,
            SourceAbstraction: source.Abstraction,
            TargetAbstraction: target.Abstraction,
            Relationship:
                CweTopologyRelationshipKind.Unrelated,
            Distance: null,
            Path: [],
            CommonAncestors: []);
    }

    public async Task<IReadOnlyList<CweTopologyNode>>
        GetAncestorsAsync(
            int cweId,
            CweAbstractionLevel? abstraction = null,
            CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        var topology =
            await LoadTopologyAsync(
                context,
                cancellationToken);

        EnsureCweExists(
            cweId,
            topology.Nodes);

        var ancestorDistances =
            FindAllAncestorDistances(
                cweId,
                topology.ParentIdsByChildId);

        return ancestorDistances
            .OrderBy(x => x.Value)
            .ThenBy(x => x.Key)
            .Select(x => topology.Nodes[x.Key])
            .Where(x =>
                abstraction is null ||
                x.Abstraction == abstraction.Value)
            .ToArray();
    }
    public static int CalculateConfidence(
    RelationshipResult relationship,
    CweTopologyMatch topology)
    {
        var semanticScore =
            relationship.Score;

        var topologyScore =
            GetTopologyScore(topology);

        return (int)Math.Round(
            (semanticScore * 0.7) +
            (topologyScore * 0.3));
    }
    private static int GetTopologyScore(
    CweTopologyMatch topology)
    {
        return topology.Relationship switch
        {
            CweTopologyRelationshipKind.SameCwe => 1000,

            CweTopologyRelationshipKind.DescendantOf
                when topology.Distance == 1 => 900,

            CweTopologyRelationshipKind.AncestorOf
                when topology.Distance == 1 => 900,

            CweTopologyRelationshipKind.DescendantOf => 800,

            CweTopologyRelationshipKind.AncestorOf => 800,

            CweTopologyRelationshipKind.SharesAncestorWith => 500,

            CweTopologyRelationshipKind.Unrelated => 0,

            CweTopologyRelationshipKind.SourceCweMissing => 0,
            CweTopologyRelationshipKind.TargetCweMissing => 0,
            CweTopologyRelationshipKind.BothCweMissing => 0,

            _ => 0
        };
    }

    public async Task<IReadOnlyList<CweTopologyNode>>
        GetDescendantsAsync(
            int cweId,
            CweAbstractionLevel? abstraction = null,
            CancellationToken cancellationToken = default)
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync(
                cancellationToken);

        var topology =
            await LoadTopologyAsync(
                context,
                cancellationToken);

        EnsureCweExists(
            cweId,
            topology.Nodes);

        var descendantDistances =
            FindAllDescendantDistances(
                cweId,
                topology.ChildIdsByParentId);

        return descendantDistances
            .OrderBy(x => x.Value)
            .ThenBy(x => x.Key)
            .Select(x => topology.Nodes[x.Key])
            .Where(x =>
                abstraction is null ||
                x.Abstraction == abstraction.Value)
            .ToArray();
    }

    private static async Task<CweTopologyGraph>
        LoadTopologyAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken)
    {
        var nodes = await context.CWECatalogs
            .AsNoTracking()
            .Select(x => new
            {
                x.CweId,
                x.Name,
                x.Abstraction
            })
            .ToListAsync(cancellationToken);

        var nodeById = nodes.ToDictionary(
            x => x.CweId,
            x => new CweTopologyNode(
                x.CweId,
                x.Name,
                ParseAbstraction(x.Abstraction)));

        /*
         * Only direct MITRE ChildOf relationships are needed.
         *
         * Your seeder already creates ParentOf inverses. Using only the
         * original ChildOf edges here prevents duplicate graph edges.
         */
        var relationships = await context.Relationships
            .AsNoTracking()
            .Where(x =>
                !x.IsDerived &&
                x.ViewId == ResearchConceptsViewId &&
                x.Nature ==
                    nameof(
                        RelatedNatureEnumeration.ChildOf))
            .Select(x => new
            {
                ChildCweId = x.CweId,
                ParentCweId = x.RelatedCweID
            })
            .ToListAsync(cancellationToken);

        var parentIdsByChildId = relationships
            .GroupBy(x => x.ChildCweId)
            .ToDictionary(
                group => group.Key,
                group => (
                    IReadOnlyCollection<int>)group
                        .Select(x => x.ParentCweId)
                        .Distinct()
                        .ToArray());

        var childIdsByParentId = relationships
            .GroupBy(x => x.ParentCweId)
            .ToDictionary(
                group => group.Key,
                group => (
                    IReadOnlyCollection<int>)group
                        .Select(x => x.ChildCweId)
                        .Distinct()
                        .ToArray());

        return new CweTopologyGraph(
            nodeById,
            parentIdsByChildId,
            childIdsByParentId);
    }

    private static IReadOnlyList<int>? FindAncestorPath(
        int startingCweId,
        int requestedAncestorCweId,
        IReadOnlyDictionary<
            int,
            IReadOnlyCollection<int>> parentIdsByChildId)
    {
        var queue = new Queue<int>();
        var visited = new HashSet<int>();
        var previous = new Dictionary<int, int>();

        queue.Enqueue(startingCweId);
        visited.Add(startingCweId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!parentIdsByChildId.TryGetValue(
                    current,
                    out var parents))
            {
                continue;
            }

            foreach (var parent in parents)
            {
                if (!visited.Add(parent))
                {
                    continue;
                }

                previous[parent] = current;

                if (parent == requestedAncestorCweId)
                {
                    return BuildPath(
                        startingCweId,
                        requestedAncestorCweId,
                        previous);
                }

                queue.Enqueue(parent);
            }
        }

        return null;
    }

    private static IReadOnlyList<int> BuildPath(
        int startingCweId,
        int requestedAncestorCweId,
        IReadOnlyDictionary<int, int> previous)
    {
        var path = new List<int>
        {
            requestedAncestorCweId
        };

        var current = requestedAncestorCweId;

        while (current != startingCweId)
        {
            if (!previous.TryGetValue(
                    current,
                    out var child))
            {
                throw new InvalidOperationException(
                    "The CWE topology path is incomplete.");
            }

            path.Add(child);
            current = child;
        }

        path.Reverse();

        return path;
    }

    private static IReadOnlyDictionary<int, int>
        FindAllAncestorDistances(
            int startingCweId,
            IReadOnlyDictionary<
                int,
                IReadOnlyCollection<int>> parentIdsByChildId)
    {
        return FindAllDistances(
            startingCweId,
            parentIdsByChildId);
    }

    private static IReadOnlyDictionary<int, int>
        FindAllDescendantDistances(
            int startingCweId,
            IReadOnlyDictionary<
                int,
                IReadOnlyCollection<int>> childIdsByParentId)
    {
        return FindAllDistances(
            startingCweId,
            childIdsByParentId);
    }

    private static IReadOnlyDictionary<int, int>
        FindAllDistances(
            int startingCweId,
            IReadOnlyDictionary<
                int,
                IReadOnlyCollection<int>> adjacentIds)
    {
        var distances = new Dictionary<int, int>();
        var queue = new Queue<(int CweId, int Distance)>();

        queue.Enqueue((startingCweId, 0));

        var visited = new HashSet<int>
        {
            startingCweId
        };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!adjacentIds.TryGetValue(
                    current.CweId,
                    out var adjacent))
            {
                continue;
            }

            foreach (var relatedCweId in adjacent)
            {
                if (!visited.Add(relatedCweId))
                {
                    continue;
                }

                var distance = current.Distance + 1;

                distances[relatedCweId] = distance;
                queue.Enqueue(
                    (relatedCweId, distance));
            }
        }

        return distances;
    }

    private static IReadOnlyList<CweTopologyNode>
        ConvertToNodes(
            IEnumerable<int> cweIds,
            IReadOnlyDictionary<int, CweTopologyNode> nodes)
    {
        return cweIds
            .Where(nodes.ContainsKey)
            .Select(id => nodes[id])
            .ToArray();
    }

    private static CweAbstractionLevel ParseAbstraction(
        string? value)
    {
        return Enum.TryParse<CweAbstractionLevel>(
            value,
            ignoreCase: true,
            out var abstraction)
                ? abstraction
                : CweAbstractionLevel.Unknown;
    }

    private static void EnsureCweExists(
        int cweId,
        IReadOnlyDictionary<int, CweTopologyNode> nodes)
    {
        if (!nodes.ContainsKey(cweId))
        {
            throw new KeyNotFoundException(
                $"CWE-{cweId} was not found.");
        }
    }

    private sealed record CweTopologyGraph(
        IReadOnlyDictionary<int, CweTopologyNode> Nodes,
        IReadOnlyDictionary<
            int,
            IReadOnlyCollection<int>> ParentIdsByChildId,
        IReadOnlyDictionary<
            int,
            IReadOnlyCollection<int>> ChildIdsByParentId);
}