using System;
using System.Collections.Generic;
using System.Text;

namespace ToolTester.Application.CweRelationshipEngine
{

    public enum CweAbstractionLevel
    {
        Unknown = 0,
        Pillar = 1,
        Class = 2,
        Base = 3,
        Variant = 4,
        Compound = 5
    }

    public enum CweTopologyRelationshipKind
    {
        SameCwe = 0,

        /// <summary>
        /// The source CWE is more specific than the target CWE.
        /// Example: Variant -> Class or Variant -> Pillar.
        /// </summary>
        DescendantOf = 1,

        /// <summary>
        /// The source CWE is more abstract than the target CWE.
        /// Example: Pillar -> Variant or Class -> Variant.
        /// </summary>
        AncestorOf = 2,

        /// <summary>
        /// Both CWEs have at least one common ancestor,
        /// but neither is an ancestor of the other.
        /// </summary>
        SharesAncestorWith = 3,

        Unrelated = 4,
        SourceCweMissing = 5,
        TargetCweMissing = 6,
        BothCweMissing = 7
    }

    public sealed record CweTopologyNode(
        int CweId,
        string Name,
        CweAbstractionLevel Abstraction);

    public sealed record CweTopologyMatch(
        int SourceCweId,
        int TargetCweId,
        CweAbstractionLevel SourceAbstraction,
        CweAbstractionLevel TargetAbstraction,
        CweTopologyRelationshipKind Relationship,
        int? Distance,
        IReadOnlyList<CweTopologyNode> Path,
        IReadOnlyList<CweTopologyNode> CommonAncestors)
    {
        public bool IsRelated =>
            Relationship != CweTopologyRelationshipKind.Unrelated;

        public bool IsDirect =>
            Distance == 1;

        public static CweTopologyMatch MissingSource(
int sourceCweId,
int targetCweId,
CweTopologyNode target)
=> new(
SourceCweId: sourceCweId,
TargetCweId: targetCweId,
SourceAbstraction:
CweAbstractionLevel.Unknown,
TargetAbstraction:
target.Abstraction,
Relationship:
CweTopologyRelationshipKind
.SourceCweMissing,
Distance: null,
Path: [],
CommonAncestors: []);

        public static CweTopologyMatch MissingTarget(
        int sourceCweId,
        int targetCweId,
        CweTopologyNode source)
        => new(
        SourceCweId: sourceCweId,
        TargetCweId: targetCweId,
        SourceAbstraction:
        source.Abstraction,
        TargetAbstraction:
        CweAbstractionLevel.Unknown,
        Relationship:
        CweTopologyRelationshipKind
        .TargetCweMissing,
        Distance: null,
        Path: [],
        CommonAncestors: []);
    }
}
