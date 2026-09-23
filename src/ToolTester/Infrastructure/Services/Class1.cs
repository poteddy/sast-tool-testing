using CweRelationshipEngine;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using ToolTester.Infrastructure.Persistance;

namespace ToolTester.Infrastructure.Services
{
    public sealed class CweRelationshipService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public CweRelationshipService(
            IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<RelationshipResult> EvaluateAsync(
            int scannerCweId,
            int groundTruthCweId,
            string? scannerRuleId = null,
            string? programmingLanguage = null,
            CancellationToken cancellationToken = default)
        {
            if (scannerCweId == groundTruthCweId)
            {
                return ExactMatch(scannerCweId);
            }

            await using var context =
                await _contextFactory.CreateDbContextAsync(
                    cancellationToken);

            var parentGraph =
                await LoadParentGraphAsync(
                    context,
                    cancellationToken);

            var hierarchyResult =
                EvaluateHierarchy(
                    scannerCweId,
                    groundTruthCweId,
                    parentGraph);

            if (hierarchyResult is not null)
            {
                return hierarchyResult;
            }

            var siblingResult =
                EvaluateSiblingRelationship(
                    scannerCweId,
                    groundTruthCweId,
                    parentGraph);

            if (siblingResult is not null)
            {
                return siblingResult;
            }

            var nativeResult =
                await EvaluateNativeRelationshipAsync(
                    context,
                    scannerCweId,
                    groundTruthCweId,
                    cancellationToken);

            if (nativeResult is not null)
            {
                return nativeResult;
            }

            var semanticResult =
                await EvaluateSemanticRelationshipAsync(
                    context,
                    scannerCweId,
                    groundTruthCweId,
                    scannerRuleId,
                    programmingLanguage,
                    cancellationToken);

            if (semanticResult is not null)
            {
                return semanticResult;
            }

            return CreateResult(
                scannerCweId,
                groundTruthCweId,
                CweRelationshipKind.Unrelated,
                0,
                [],
                "No supported relationship was found.");
        }

        private static RelationshipResult ExactMatch(
            int cweId)
        {
            return CreateResult(
                cweId,
                cweId,
                CweRelationshipKind.Exact,
                1000,
                [cweId],
                "The scanner CWE exactly matches the ground truth CWE.");
        }

        private static async Task<
            IReadOnlyDictionary<int, HashSet<int>>>
            LoadParentGraphAsync(
                ApplicationDbContext context,
                CancellationToken cancellationToken)
        {
            var relationships = await context.Relationships
                .AsNoTracking()
                .Where(x =>
                    x.Nature ==
                        nameof(RelatedNatureEnumeration.ChildOf) &&
                    !x.IsDerived)
                .Select(x => new
                {
                    Child = x.CweId,
                    Parent = x.RelatedCweID
                })
                .ToListAsync(cancellationToken);

            return relationships
                .GroupBy(x => x.Child)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Parent).ToHashSet());
        }

        private static RelationshipResult? EvaluateHierarchy(
            int scannerCweId,
            int truthCweId,
            IReadOnlyDictionary<int, HashSet<int>>
                parentGraph)
        {
            var scannerToTruth =
                FindParentPath(
                    scannerCweId,
                    truthCweId,
                    parentGraph,
                    10);

            if (scannerToTruth is { Count: 2 })
            {
                return CreateResult(
                    scannerCweId,
                    truthCweId,
                    CweRelationshipKind.DirectChild,
                    925,
                    scannerToTruth,
                    "Scanner CWE is a direct child of ground truth.");
            }

            var truthToScanner =
                FindParentPath(
                    truthCweId,
                    scannerCweId,
                    parentGraph,
                    10);

            if (truthToScanner is { Count: 2 })
            {
                return CreateResult(
                    scannerCweId,
                    truthCweId,
                    CweRelationshipKind.DirectParent,
                    900,
                    truthToScanner.Reverse().ToArray(),
                    "Scanner CWE is a direct parent of ground truth.");
            }

            if (scannerToTruth is { Count: > 2 })
            {
                var distance = scannerToTruth.Count - 1;

                return CreateResult(
                    scannerCweId,
                    truthCweId,
                    distance == 2
                        ? CweRelationshipKind.GrandChild
                        : CweRelationshipKind.Descendant,
                    ScoreHierarchyDistance(distance, true),
                    scannerToTruth,
                    $"Scanner CWE is a descendant (distance {distance}).");
            }

            if (truthToScanner is { Count: > 2 })
            {
                var distance = truthToScanner.Count - 1;

                return CreateResult(
                    scannerCweId,
                    truthCweId,
                    distance == 2
                        ? CweRelationshipKind.GrandParent
                        : CweRelationshipKind.Ancestor,
                    ScoreHierarchyDistance(distance, false),
                    truthToScanner.Reverse().ToArray(),
                    $"Scanner CWE is an ancestor (distance {distance}).");
            }

            return null;
        }
        private static IReadOnlyList<int>? FindParentPath(
    int startCweId,
    int targetCweId,
    IReadOnlyDictionary<int, HashSet<int>> parentsByChild,
    int maximumDepth = 10)
        {
            if (startCweId == targetCweId)
            {
                return [startCweId];
            }

            var visited = new HashSet<int>
    {
        startCweId
    };

            var queue = new Queue<List<int>>();

            queue.Enqueue(
            [
                startCweId
            ]);

            while (queue.Count > 0)
            {
                var currentPath = queue.Dequeue();

                if (currentPath.Count - 1 >= maximumDepth)
                {
                    continue;
                }

                var currentCwe = currentPath[^1];

                if (!parentsByChild.TryGetValue(
                        currentCwe,
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

                    var nextPath =
                        new List<int>(currentPath)
                        {
                    parent
                        };

                    if (parent == targetCweId)
                    {
                        return nextPath;
                    }

                    queue.Enqueue(nextPath);
                }
            }

            return null;
        }
        private static RelationshipResult? EvaluateSiblingRelationship(
            int scannerCweId,
            int truthCweId,
            IReadOnlyDictionary<int, HashSet<int>>
                parentGraph)
        {
            var scannerParents =
                parentGraph.GetValueOrDefault(
                    scannerCweId,
                    []);

            var truthParents =
                parentGraph.GetValueOrDefault(
                    truthCweId,
                    []);

            var commonParent = scannerParents
                .Intersect(truthParents)
                .FirstOrDefault();

            if (commonParent == 0)
            {
                return null;
            }

            return CreateResult(
                scannerCweId,
                truthCweId,
                CweRelationshipKind.DirectSibling,
                800,
                [scannerCweId, commonParent, truthCweId],
                $"Both CWEs share parent CWE-{commonParent}.");
        }

        private static async Task<RelationshipResult?>
            EvaluateNativeRelationshipAsync(
                ApplicationDbContext context,
                int scannerCweId,
                int truthCweId,
                CancellationToken cancellationToken)
        {
            var native = await context.Relationships
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.CweId == scannerCweId &&
                        x.RelatedCweID == truthCweId &&
                        x.Nature != nameof(RelatedNatureEnumeration.ChildOf) &&
                        x.Nature != nameof(RelatedNatureEnumeration.ParentOf),
                    cancellationToken);

            if (native is null)
            {
                return null;
            }

            return CreateResult(
                scannerCweId,
                truthCweId,
                ParseNativeRelationship(native.Nature),
                ScoreNativeRelationship(native.Nature),
                [scannerCweId, truthCweId],
                $"MITRE relationship: {native.Nature}");
        }

        private static async Task<RelationshipResult?>
            EvaluateSemanticRelationshipAsync(
                ApplicationDbContext context,
                int scannerCweId,
                int truthCweId,
                string? scannerRuleId,
                string? programmingLanguage,
                CancellationToken cancellationToken)
        {
            var rule = await context.CweSemanticRules
                .AsNoTracking()
                .Where(x =>
                    x.Enabled &&
                    x.SourceCweId == scannerCweId &&
                    x.TargetCweId == truthCweId)
                .Where(x =>
                    x.ScannerRuleId == null ||
                    x.ScannerRuleId == scannerRuleId)
                .Where(x =>
                    x.ProgrammingLanguage == null ||
                    x.ProgrammingLanguage == programmingLanguage)
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync(cancellationToken);

            if (rule is null)
            {
                return null;
            }

            return CreateResult(
                scannerCweId,
                truthCweId,
                Enum.Parse<CweRelationshipKind>(
                    rule.Relationship),
                rule.Score,
                [],
                rule.Rationale);
        }

        private static int ScoreHierarchyDistance(
            int distance,
            bool scannerIsSpecific)
        {
            var score = distance switch
            {
                1 => 925,
                2 => 825,
                3 => 750,
                4 => 675,
                _ => Math.Max(500, 675 - ((distance - 4) * 50))
            };

            return scannerIsSpecific
                ? score
                : Math.Max(0, score - 25);
        }
        private static int ScoreNativeRelationship(
    string nature)
        {
            return nature switch
            {
                nameof(RelatedNatureEnumeration.CanPrecede) => 775,
                nameof(RelatedNatureEnumeration.CanFollow) => 750,

                nameof(RelatedNatureEnumeration.Requires) => 725,

                nameof(RelatedNatureEnumeration.PeerOf) => 700,

                nameof(RelatedNatureEnumeration.CanAlsoBe) => 700,

                _ => 600
            };
        }
        private static RelationshipResult CreateResult(
    int scannerCweId,
    int groundTruthCweId,
    CweRelationshipKind relationship,
    int score,
    IReadOnlyList<int> path,
    string explanation)
        {
            var classification = score switch
            {
                >= 750 => MatchClassification.TruePositive,
                >= 500 => MatchClassification.PartialTruePositive,
                >= 250 => MatchClassification.NeedsReview,
                _ => MatchClassification.FalsePositive
            };

            return new RelationshipResult(
                ScannerCweId: scannerCweId,
                GroundTruthCweId: groundTruthCweId,
                Relationship: relationship,
                Score: score,
                Classification: classification,
                Path: path,
                Explanation: explanation);
        }
        private static CweRelationshipKind ParseNativeRelationship(
    string nature)
        {
            return nature switch
            {
                nameof(RelatedNatureEnumeration.PeerOf)
                    => CweRelationshipKind.PeerOf,

                nameof(RelatedNatureEnumeration.CanPrecede)
                    => CweRelationshipKind.CanPrecede,

                nameof(RelatedNatureEnumeration.CanFollow)
                    => CweRelationshipKind.CanFollow,

                nameof(RelatedNatureEnumeration.Requires)
                    => CweRelationshipKind.Requires,

                nameof(RelatedNatureEnumeration.CanAlsoBe)
                    => CweRelationshipKind.CanAlsoBe,

                nameof(RelatedNatureEnumeration.ParentOf)
                    => CweRelationshipKind.DirectParent,

                nameof(RelatedNatureEnumeration.ChildOf)
                    => CweRelationshipKind.DirectChild,

                _ => CweRelationshipKind.RelatedWeakness
            };
        }
    }
    public sealed record RelationshipResult(
    int ScannerCweId,
    int GroundTruthCweId,
    CweRelationshipKind Relationship,
    int Score,
    MatchClassification Classification,
    IReadOnlyList<int> Path,
    string Explanation);
}
