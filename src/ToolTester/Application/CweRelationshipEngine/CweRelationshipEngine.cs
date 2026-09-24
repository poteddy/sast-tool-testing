using System.Collections.ObjectModel;
using System.Xml.Linq;
public enum CweRelationshipKind
{
    Exact,

    // Direction is always Scanner CWE -> Ground-truth CWE.
    DirectParent,
    DirectChild,
    Ancestor,
    Descendant,
    DirectSibling,
    SharedAncestor,

    // Native non-hierarchical MITRE relationships.
    CanPrecede,
    CanFollow,
    PeerOf,
    Requires,
    CanAlsoBe,

    // Custom, derived semantic relationships.
    SameRootCauseBroaderCwe,
    SameRootCauseNarrowerCwe,
    SameConsequence,
    SameWeaknessFamily,
    RelatedAttackPattern,

    Unrelated,
    GrandChild,
    GrandParent,
    RelatedWeakness,
}

public enum EvidenceSource
{
    ExactIdentifier,
    MitreHierarchy,
    MitreRelationship,
    CustomSemanticRule,
    None
}

public enum MatchClassification
{
    TruePositive,
    PartialTruePositive,
    NeedsReview,
    FalsePositive
}

public sealed record CweNode(
    int Id,
    string Name,
    string? Abstraction,
    string? Status);

public sealed record CweEdge(
    int SourceId,
    int TargetId,
    string Nature,
    int? ViewId,
    string? Ordinal);

public sealed record SemanticRule(
    int SourceCweId,
    int TargetCweId,
    CweRelationshipKind Relationship,
    int Score,
    string Rationale,
    string EvidenceReference,
    string? ScannerRuleId = null,
    string? ProgrammingLanguage = null,
    bool Bidirectional = false,
    bool Enabled = true,
    int Version = 1);

public sealed record RelationshipResult(
    int ScannerCweId,
    int GroundTruthCweId,
    CweRelationshipKind Relationship,
    int Score,
    MatchClassification Classification,
    EvidenceSource EvidenceSource,
    IReadOnlyList<int> Path,
    string Explanation,
    string? EvidenceReference);

public sealed class CweGraph
{
    private const string ChildOf = "ChildOf";

    private readonly Dictionary<int, CweNode> _nodes = [];
    private readonly List<CweEdge> _edges = [];

    // child -> parents
    private readonly Dictionary<int, HashSet<int>> _parents = [];

    // parent -> children
    private readonly Dictionary<int, HashSet<int>> _children = [];

    private readonly Dictionary<(int Source, int Target), List<CweEdge>>
        _edgesByPair = [];

    public IReadOnlyDictionary<int, CweNode> Nodes =>
        new ReadOnlyDictionary<int, CweNode>(_nodes);

    public IReadOnlyList<CweEdge> Edges => _edges.AsReadOnly();

    public void AddNode(CweNode node)
    {
        _nodes[node.Id] = node;
    }

    public void AddEdge(CweEdge edge)
    {
        if (!_nodes.ContainsKey(edge.SourceId) ||
            !_nodes.ContainsKey(edge.TargetId))
        {
            return;
        }

        _edges.Add(edge);

        var key = (edge.SourceId, edge.TargetId);

        if (!_edgesByPair.TryGetValue(key, out var pairEdges))
        {
            pairEdges = [];
            _edgesByPair[key] = pairEdges;
        }

        pairEdges.Add(edge);

        /*
         * MITRE XML generally represents hierarchy as:
         *
         * source CWE --ChildOf--> target CWE
         *
         * Thus SourceId is the child, and TargetId is the parent.
         */
        if (edge.Nature.Equals(
                ChildOf,
                StringComparison.OrdinalIgnoreCase))
        {
            AddToSet(_parents, edge.SourceId, edge.TargetId);
            AddToSet(_children, edge.TargetId, edge.SourceId);
        }
    }

    public bool ContainsCwe(int cweId) => _nodes.ContainsKey(cweId);

    public CweNode GetNode(int cweId) =>
        _nodes.TryGetValue(cweId, out var node)
            ? node
            : throw new KeyNotFoundException($"CWE-{cweId} was not loaded.");

    public IReadOnlyCollection<int> GetParents(int cweId) =>
        _parents.TryGetValue(cweId, out var values)
            ? values
            : Array.Empty<int>();

    public IReadOnlyCollection<int> GetChildren(int cweId) =>
        _children.TryGetValue(cweId, out var values)
            ? values
            : Array.Empty<int>();

    public IReadOnlyList<CweEdge> GetEdges(
        int sourceId,
        int targetId)
    {
        return _edgesByPair.TryGetValue(
            (sourceId, targetId),
            out var edges)
                ? edges.AsReadOnly()
                : [];
    }

    public IReadOnlyList<int>? FindAncestorPath(
        int descendantId,
        int ancestorId,
        int maximumDepth = 10)
    {
        return FindPath(
            descendantId,
            ancestorId,
            GetParents,
            maximumDepth);
    }

    public IReadOnlyList<int>? FindDescendantPath(
        int ancestorId,
        int descendantId,
        int maximumDepth = 10)
    {
        return FindPath(
            ancestorId,
            descendantId,
            GetChildren,
            maximumDepth);
    }

    public IReadOnlyCollection<int> GetAncestors(
        int cweId,
        int maximumDepth = 10)
    {
        return Traverse(cweId, GetParents, maximumDepth);
    }

    public IReadOnlyCollection<int> GetDescendants(
        int cweId,
        int maximumDepth = 10)
    {
        return Traverse(cweId, GetChildren, maximumDepth);
    }

    public IReadOnlyCollection<int> GetCommonDirectParents(
        int firstCweId,
        int secondCweId)
    {
        var firstParents = GetParents(firstCweId);

        return firstParents
            .Intersect(GetParents(secondCweId))
            .Order()
            .ToArray();
    }

    public (int AncestorId, int TotalDistance)? FindNearestCommonAncestor(
        int firstCweId,
        int secondCweId,
        int maximumDepth = 10)
    {
        var firstDistances =
            GetAncestorDistances(firstCweId, maximumDepth);

        var secondDistances =
            GetAncestorDistances(secondCweId, maximumDepth);

        return firstDistances.Keys
            .Intersect(secondDistances.Keys)
            .Select(id => new
            {
                Id = id,
                Distance =
                    firstDistances[id] + secondDistances[id]
            })
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Id)
            .Select(x => ((int AncestorId, int TotalDistance)?)
                (x.Id, x.Distance))
            .FirstOrDefault();
    }

    private Dictionary<int, int> GetAncestorDistances(
        int cweId,
        int maximumDepth)
    {
        var distances = new Dictionary<int, int>();
        var queue = new Queue<(int Id, int Distance)>();

        queue.Enqueue((cweId, 0));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.Distance >= maximumDepth)
            {
                continue;
            }

            foreach (var parent in GetParents(current.Id))
            {
                var distance = current.Distance + 1;

                if (distances.TryGetValue(parent, out var existing) &&
                    existing <= distance)
                {
                    continue;
                }

                distances[parent] = distance;
                queue.Enqueue((parent, distance));
            }
        }

        return distances;
    }

    private static IReadOnlyList<int>? FindPath(
        int startId,
        int targetId,
        Func<int, IReadOnlyCollection<int>> next,
        int maximumDepth)
    {
        if (startId == targetId)
        {
            return [startId];
        }

        var visited = new HashSet<int> { startId };
        var queue = new Queue<List<int>>();
        queue.Enqueue([startId]);

        while (queue.Count > 0)
        {
            var path = queue.Dequeue();

            if (path.Count - 1 >= maximumDepth)
            {
                continue;
            }

            var current = path[^1];

            foreach (var candidate in next(current))
            {
                if (!visited.Add(candidate))
                {
                    continue;
                }

                var nextPath = new List<int>(path) { candidate };

                if (candidate == targetId)
                {
                    return nextPath;
                }

                queue.Enqueue(nextPath);
            }
        }

        return null;
    }

    private static IReadOnlyCollection<int> Traverse(
        int startId,
        Func<int, IReadOnlyCollection<int>> next,
        int maximumDepth)
    {
        var result = new HashSet<int>();
        var queue = new Queue<(int Id, int Depth)>();
        queue.Enqueue((startId, 0));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.Depth >= maximumDepth)
            {
                continue;
            }

            foreach (var candidate in next(current.Id))
            {
                if (!result.Add(candidate))
                {
                    continue;
                }

                queue.Enqueue((candidate, current.Depth + 1));
            }
        }

        return result;
    }

    private static void AddToSet(
        Dictionary<int, HashSet<int>> dictionary,
        int key,
        int value)
    {
        if (!dictionary.TryGetValue(key, out var values))
        {
            values = [];
            dictionary[key] = values;
        }

        values.Add(value);
    }
}

public static class MitreCweXmlLoader
{
    public static CweGraph Load(string xmlPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlPath);

        if (!File.Exists(xmlPath))
        {
            throw new FileNotFoundException(
                "The MITRE CWE XML file was not found.",
                xmlPath);
        }

        var document = XDocument.Load(
            xmlPath,
            LoadOptions.None);

        var graph = new CweGraph();

        /*
         * LocalName is used deliberately so the importer continues
         * working when MITRE changes the XML namespace URI.
         */
        var weaknessElements = document
            .Descendants()
            .Where(element =>
                element.Name.LocalName == "Weakness")
            .ToArray();

        foreach (var weakness in weaknessElements)
        {
            if (!TryReadIntAttribute(weakness, "ID", out var id))
            {
                continue;
            }

            graph.AddNode(new CweNode(
                Id: id,
                Name: ReadAttribute(weakness, "Name") ?? $"CWE-{id}",
                Abstraction: ReadAttribute(weakness, "Abstraction"),
                Status: ReadAttribute(weakness, "Status")));
        }

        foreach (var weakness in weaknessElements)
        {
            if (!TryReadIntAttribute(weakness, "ID", out var sourceId))
            {
                continue;
            }

            var relatedWeaknesses = weakness
                .Descendants()
                .Where(element =>
                    element.Name.LocalName == "Related_Weakness");

            foreach (var related in relatedWeaknesses)
            {
                if (!TryReadIntAttribute(
                        related,
                        "CWE_ID",
                        out var targetId))
                {
                    continue;
                }

                var nature =
                    ReadAttribute(related, "Nature") ??
                    "Unknown";

                int? viewId = TryReadIntAttribute(
                    related,
                    "View_ID",
                    out var parsedViewId)
                        ? parsedViewId
                        : null;

                graph.AddEdge(new CweEdge(
                    SourceId: sourceId,
                    TargetId: targetId,
                    Nature: nature,
                    ViewId: viewId,
                    Ordinal: ReadAttribute(related, "Ordinal")));
            }
        }

        return graph;
    }

    private static string? ReadAttribute(
        XElement element,
        string localName)
    {
        return element.Attributes()
            .FirstOrDefault(attribute =>
                attribute.Name.LocalName.Equals(
                    localName,
                    StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    private static bool TryReadIntAttribute(
        XElement element,
        string localName,
        out int value)
    {
        var text = ReadAttribute(element, localName);
        return int.TryParse(text, out value);
    }
}

public sealed class CweRelationshipEngine
{
    private readonly CweGraph _graph;

    private readonly Dictionary<(int Source, int Target), SemanticRule>
        _semanticRules;

    public CweRelationshipEngine(
        CweGraph graph,
        IEnumerable<SemanticRule>? semanticRules = null)
    {
        _graph = graph ??
            throw new ArgumentNullException(nameof(graph));

        _semanticRules = [];

        foreach (var rule in semanticRules ?? [])
        {
            AddSemanticRule(rule);
        }
    }

    public RelationshipResult Evaluate(
        int scannerCweId,
        int groundTruthCweId)
    {
        EnsureKnown(scannerCweId);
        EnsureKnown(groundTruthCweId);

        // Rule 1: exact identifier.
        if (scannerCweId == groundTruthCweId)
        {
            return CreateResult(
                scannerCweId,
                groundTruthCweId,
                CweRelationshipKind.Exact,
                1000,
                EvidenceSource.ExactIdentifier,
                [scannerCweId],
                "The scanner and ground truth report the same CWE.",
                null);
        }

        /*
         * Rule 2: direct hierarchy.
         *
         * Because ChildOf is represented as child -> parent:
         *
         * scanner path to truth means scanner is the child.
         * truth path to scanner means scanner is the parent.
         */
        var scannerToTruth =
            _graph.FindAncestorPath(
                scannerCweId,
                groundTruthCweId);

        if (scannerToTruth is { Count: 2 })
        {
            return CreateResult(
                scannerCweId,
                groundTruthCweId,
                CweRelationshipKind.DirectChild,
                925,
                EvidenceSource.MitreHierarchy,
                scannerToTruth,
                "The scanner CWE is a direct child of the ground-truth CWE.",
                "MITRE Related_Weakness Nature=ChildOf");
        }

        var truthToScanner =
            _graph.FindAncestorPath(
                groundTruthCweId,
                scannerCweId);

        if (truthToScanner is { Count: 2 })
        {
            return CreateResult(
                scannerCweId,
                groundTruthCweId,
                CweRelationshipKind.DirectParent,
                900,
                EvidenceSource.MitreHierarchy,
                truthToScanner.Reverse().ToArray(),
                "The scanner CWE is a direct parent of the ground-truth CWE.",
                "MITRE Related_Weakness Nature=ChildOf");
        }

        // Rule 3: transitive hierarchy.
        if (scannerToTruth is { Count: > 2 })
        {
            var edgeDistance = scannerToTruth.Count - 1;
            var score = ScoreAncestorDistance(edgeDistance);

            return CreateResult(
                scannerCweId,
                groundTruthCweId,
                CweRelationshipKind.Descendant,
                score,
                EvidenceSource.MitreHierarchy,
                scannerToTruth,
                $"The scanner CWE is a descendant of the ground-truth CWE at distance {edgeDistance}.",
                "Derived transitively from MITRE ChildOf edges");
        }

        if (truthToScanner is { Count: > 2 })
        {
            var edgeDistance = truthToScanner.Count - 1;
            var score = ScoreAncestorDistance(edgeDistance) - 25;

            return CreateResult(
                scannerCweId,
                groundTruthCweId,
                CweRelationshipKind.Ancestor,
                score,
                EvidenceSource.MitreHierarchy,
                truthToScanner.Reverse().ToArray(),
                $"The scanner CWE is an ancestor of the ground-truth CWE at distance {edgeDistance}.",
                "Derived transitively from MITRE ChildOf edges");
        }

        // Rule 4: direct siblings.
        var commonParents =
            _graph.GetCommonDirectParents(
                scannerCweId,
                groundTruthCweId);

        if (commonParents.Count > 0)
        {
            var parent = commonParents.Min();

            return CreateResult(
                scannerCweId,
                groundTruthCweId,
                CweRelationshipKind.DirectSibling,
                800,
                EvidenceSource.MitreHierarchy,
                [scannerCweId, parent, groundTruthCweId],
                $"The CWEs share direct parent CWE-{parent}.",
                "Derived from MITRE ChildOf edges");
        }

        // Rule 5: native, non-hierarchical MITRE relations.
        var nativeRelationship =
            TryResolveNativeMitreRelationship(
                scannerCweId,
                groundTruthCweId);

        if (nativeRelationship is not null)
        {
            return nativeRelationship;
        }

        /*
         * Rule 6: explicit semantic overrides.
         *
         * Custom semantic rules take precedence over a generic shared
         * ancestor because they are expected to carry reviewed evidence.
         */
        if (_semanticRules.TryGetValue(
                (scannerCweId, groundTruthCweId),
                out var semanticRule))
        {
            return CreateResult(
                scannerCweId,
                groundTruthCweId,
                semanticRule.Relationship,
                semanticRule.Score,
                EvidenceSource.CustomSemanticRule,
                [],
                semanticRule.Rationale,
                semanticRule.EvidenceReference);
        }

        // Rule 7: broader common ancestry.
        var commonAncestor =
            _graph.FindNearestCommonAncestor(
                scannerCweId,
                groundTruthCweId);

        if (commonAncestor is not null)
        {
            var score = commonAncestor.Value.TotalDistance switch
            {
                <= 3 => 725,
                4 => 675,
                5 => 625,
                _ => 550
            };

            return CreateResult(
                scannerCweId,
                groundTruthCweId,
                CweRelationshipKind.SharedAncestor,
                score,
                EvidenceSource.MitreHierarchy,
                [commonAncestor.Value.AncestorId],
                $"The nearest common ancestor is CWE-{commonAncestor.Value.AncestorId}; combined graph distance is {commonAncestor.Value.TotalDistance}.",
                "Derived from MITRE ChildOf edges");
        }

        return CreateResult(
            scannerCweId,
            groundTruthCweId,
            CweRelationshipKind.Unrelated,
            0,
            EvidenceSource.None,
            [],
            "No configured taxonomy or semantic relationship was found.",
            null);
    }

    private RelationshipResult? TryResolveNativeMitreRelationship(
        int scannerCweId,
        int groundTruthCweId)
    {
        var forwardEdges =
            _graph.GetEdges(scannerCweId, groundTruthCweId);

        foreach (var edge in forwardEdges)
        {
            var relationship = MapNativeNature(edge.Nature);

            if (relationship is null)
            {
                continue;
            }

            return CreateResult(
                scannerCweId,
                groundTruthCweId,
                relationship.Value.Kind,
                relationship.Value.Score,
                EvidenceSource.MitreRelationship,
                [scannerCweId, groundTruthCweId],
                $"MITRE records {edge.Nature} from CWE-{scannerCweId} to CWE-{groundTruthCweId}.",
                $"MITRE Related_Weakness Nature={edge.Nature}, View_ID={edge.ViewId?.ToString() ?? "unspecified"}");
        }

        var reverseEdges =
            _graph.GetEdges(groundTruthCweId, scannerCweId);

        foreach (var edge in reverseEdges)
        {
            var reversed = ReverseNativeNature(edge.Nature);

            if (reversed is null)
            {
                continue;
            }

            return CreateResult(
                scannerCweId,
                groundTruthCweId,
                reversed.Value.Kind,
                reversed.Value.Score,
                EvidenceSource.MitreRelationship,
                [scannerCweId, groundTruthCweId],
                $"MITRE records the reverse relationship {edge.Nature} from CWE-{groundTruthCweId} to CWE-{scannerCweId}.",
                $"MITRE Related_Weakness Nature={edge.Nature}, View_ID={edge.ViewId?.ToString() ?? "unspecified"}");
        }

        return null;
    }

    private static (CweRelationshipKind Kind, int Score)?
        MapNativeNature(string nature)
    {
        return nature.ToUpperInvariant() switch
        {
            "CANPRECEDE" =>
                (CweRelationshipKind.CanPrecede, 775),

            "CANFOLLOW" =>
                (CweRelationshipKind.CanFollow, 750),

            "PEEROF" =>
                (CweRelationshipKind.PeerOf, 700),

            "REQUIRES" =>
                (CweRelationshipKind.Requires, 725),

            "CANALSOBE" =>
                (CweRelationshipKind.CanAlsoBe, 700),

            _ => null
        };
    }

    private static (CweRelationshipKind Kind, int Score)?
        ReverseNativeNature(string nature)
    {
        return nature.ToUpperInvariant() switch
        {
            "CANPRECEDE" =>
                (CweRelationshipKind.CanFollow, 750),

            "CANFOLLOW" =>
                (CweRelationshipKind.CanPrecede, 775),

            "PEEROF" =>
                (CweRelationshipKind.PeerOf, 700),

            /*
             * REQUIRES is directional. A reverse "required by" enum
             * could be added if that distinction is needed.
             */
            "CANALSOBE" =>
                (CweRelationshipKind.CanAlsoBe, 700),

            _ => null
        };
    }

    private void AddSemanticRule(SemanticRule rule)
    {
        ValidateScore(rule.Score);

        _semanticRules[(rule.SourceCweId, rule.TargetCweId)] = rule;

        if (!rule.Bidirectional)
        {
            return;
        }

        var reverseRelationship = rule.Relationship switch
        {
            CweRelationshipKind.SameRootCauseBroaderCwe =>
                CweRelationshipKind.SameRootCauseNarrowerCwe,

            CweRelationshipKind.SameRootCauseNarrowerCwe =>
                CweRelationshipKind.SameRootCauseBroaderCwe,

            _ => rule.Relationship
        };

        _semanticRules[(rule.TargetCweId, rule.SourceCweId)] =
            rule with
            {
                SourceCweId = rule.TargetCweId,
                TargetCweId = rule.SourceCweId,
                Relationship = reverseRelationship
            };
    }

    private static int ScoreAncestorDistance(int edgeDistance)
    {
        return edgeDistance switch
        {
            1 => 925,
            2 => 825,
            3 => 750,
            4 => 675,
            _ => Math.Max(500, 750 - ((edgeDistance - 3) * 50))
        };
    }

    private static RelationshipResult CreateResult(
        int scannerCweId,
        int groundTruthCweId,
        CweRelationshipKind relationship,
        int score,
        EvidenceSource evidenceSource,
        IReadOnlyList<int> path,
        string explanation,
        string? evidenceReference)
    {
        ValidateScore(score);

        return new RelationshipResult(
            ScannerCweId: scannerCweId,
            GroundTruthCweId: groundTruthCweId,
            Relationship: relationship,
            Score: score,
            Classification: Classify(score),
            EvidenceSource: evidenceSource,
            Path: path,
            Explanation: explanation,
            EvidenceReference: evidenceReference);
    }

    private static MatchClassification Classify(int score)
    {
        return score switch
        {
            >= 750 => MatchClassification.TruePositive,
            >= 500 => MatchClassification.PartialTruePositive,
            >= 250 => MatchClassification.NeedsReview,
            _ => MatchClassification.FalsePositive
        };
    }

    private void EnsureKnown(int cweId)
    {
        if (!_graph.ContainsCwe(cweId))
        {
            throw new ArgumentOutOfRangeException(
                nameof(cweId),
                cweId,
                $"CWE-{cweId} was not found in the loaded catalog.");
        }
    }

    private static void ValidateScore(int score)
    {
        if (score is < 0 or > 1000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(score),
                score,
                "CWE confidence scores must be between 0 and 1000.");
        }
    }
}

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine(
                "Usage: CweRelationshipEngine <cwe.xml> <scanner-cwe> <ground-truth-cwe>");
            Console.Error.WriteLine(
                "Example: CweRelationshipEngine cwec.xml 676 121");

            return 1;
        }

        if (!int.TryParse(args[1], out var scannerCweId) ||
            !int.TryParse(args[2], out var groundTruthCweId))
        {
            Console.Error.WriteLine(
                "Scanner and ground-truth CWE values must be integers.");

            return 2;
        }

        try
        {
            var graph = MitreCweXmlLoader.Load(args[0]);

            /*
             * IMPORTANT:
             *
             * This is a custom normalization rule, not a relationship
             * asserted by MITRE.
             *
             * Keep such rules in a reviewed JSON/database table in
             * production, with evidence and version history.
             */
            SemanticRule[] semanticRules =
            [
                new(
                    SourceCweId: 676,
                    TargetCweId: 121,
                    Relationship:
                        CweRelationshipKind.SameRootCauseBroaderCwe,
                    Score: 850,
                    Rationale:
                        "The scanner reported dangerous-function usage while the benchmark expects a stack-based buffer overflow. Treat this as a reviewed causal mapping only when finding-level evidence shows that the reported function use is the mechanism for the expected overflow.",
                    EvidenceReference:
                        "Internal normalization rule CWE-676-to-CWE-121 v1",
                    Bidirectional: true)
            ];

            var engine = new CweRelationshipEngine(
                graph,
                semanticRules);

            var result = engine.Evaluate(
                scannerCweId,
                groundTruthCweId);

            PrintResult(graph, result);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 3;
        }
    }

    private static void PrintResult(
        CweGraph graph,
        RelationshipResult result)
    {
        var scanner = graph.GetNode(result.ScannerCweId);
        var truth = graph.GetNode(result.GroundTruthCweId);

        Console.WriteLine(
            $"Scanner:       CWE-{scanner.Id} {scanner.Name}");

        Console.WriteLine(
            $"Ground truth:  CWE-{truth.Id} {truth.Name}");

        Console.WriteLine(
            $"Relationship:  {result.Relationship}");

        Console.WriteLine(
            $"Score:         {result.Score}");

        Console.WriteLine(
            $"Classification:{result.Classification}");

        Console.WriteLine(
            $"Evidence:      {result.EvidenceSource}");

        if (result.Path.Count > 0)
        {
            Console.WriteLine(
                $"Path:          {string.Join(" -> ", result.Path.Select(id => $"CWE-{id}"))}");
        }

        Console.WriteLine(
            $"Explanation:   {result.Explanation}");

        if (!string.IsNullOrWhiteSpace(result.EvidenceReference))
        {
            Console.WriteLine(
                $"Reference:     {result.EvidenceReference}");
        }
    }
}
