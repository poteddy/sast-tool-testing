using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolTester.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CWECatalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CweId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Abstraction = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CWECatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CweSemanticRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceCweId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetCweId = table.Column<int>(type: "INTEGER", nullable: false),
                    Relationship = table.Column<string>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    Rationale = table.Column<string>(type: "TEXT", nullable: false),
                    EvidenceReference = table.Column<string>(type: "TEXT", nullable: false),
                    ScannerRuleId = table.Column<string>(type: "TEXT", nullable: true),
                    ProgrammingLanguage = table.Column<string>(type: "TEXT", nullable: true),
                    Bidirectional = table.Column<bool>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CweSemanticRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JulietCoverages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CweId = table.Column<int>(type: "INTEGER", nullable: false),
                    Covered = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JulietCoverages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Relationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CweId = table.Column<int>(type: "INTEGER", nullable: false),
                    RelatedCweID = table.Column<int>(type: "INTEGER", nullable: false),
                    Nature = table.Column<string>(type: "TEXT", nullable: false),
                    Ordinal = table.Column<string>(type: "TEXT", nullable: true),
                    OrderSpecified = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChainId = table.Column<string>(type: "TEXT", nullable: true),
                    ViewId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsDerived = table.Column<bool>(type: "INTEGER", nullable: false),
                    Distance = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tools",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Format = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Scans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ToolId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scans_Tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "Tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CWETestResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    NumericalSeverity = table.Column<string>(type: "TEXT", nullable: false),
                    FoundBy = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    StaticFinding = table.Column<bool>(type: "INTEGER", nullable: false),
                    DynamicFinding = table.Column<bool>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: true),
                    Line = table.Column<int>(type: "INTEGER", nullable: true),
                    References = table.Column<string>(type: "TEXT", nullable: false),
                    VulnIdFromTool = table.Column<string>(type: "TEXT", nullable: false),
                    Cve = table.Column<string>(type: "TEXT", nullable: false),
                    Mitigation = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Test = table.Column<int>(type: "INTEGER", nullable: false),
                    TestPathListedCWE = table.Column<int>(type: "INTEGER", nullable: false),
                    ScanId = table.Column<int>(type: "INTEGER", nullable: false),
                    ScannerFoundCWE = table.Column<int>(type: "INTEGER", nullable: false),
                    RootCauseCWE = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CWETestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CWETestResults_Scans_ScanId",
                        column: x => x.ScanId,
                        principalTable: "Scans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScanId = table.Column<int>(type: "INTEGER", nullable: false),
                    ToolId = table.Column<int>(type: "INTEGER", nullable: false),
                    GroundTruthCweId = table.Column<int>(type: "INTEGER", nullable: false),
                    ScannerCweId = table.Column<int>(type: "INTEGER", nullable: false),
                    RootCauseCweId = table.Column<int>(type: "INTEGER", nullable: true),
                    RootCauseMatchesGroundTruth = table.Column<bool>(type: "INTEGER", nullable: false),
                    RootCauseMatchesScanner = table.Column<bool>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    Relationship = table.Column<string>(type: "TEXT", nullable: false),
                    RelationshipScore = table.Column<int>(type: "INTEGER", nullable: false),
                    TopologyRelationship = table.Column<string>(type: "TEXT", nullable: false),
                    GroundTruthAbstraction = table.Column<string>(type: "TEXT", nullable: false),
                    ScannerAbstraction = table.Column<string>(type: "TEXT", nullable: false),
                    TopologyDistance = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Scans_ScanId",
                        column: x => x.ScanId,
                        principalTable: "Scans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reports_Tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "Tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CWECatalogs_CweId",
                table: "CWECatalogs",
                column: "CweId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CweSemanticRules_SourceCweId_TargetCweId_Relationship_ScannerRuleId_ProgrammingLanguage_Version",
                table: "CweSemanticRules",
                columns: new[] { "SourceCweId", "TargetCweId", "Relationship", "ScannerRuleId", "ProgrammingLanguage", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CWETestResults_ScanId",
                table: "CWETestResults",
                column: "ScanId");

            migrationBuilder.CreateIndex(
                name: "IX_CWETestResults_ScanId_Test",
                table: "CWETestResults",
                columns: new[] { "ScanId", "Test" });

            migrationBuilder.CreateIndex(
                name: "IX_JulietCoverages_CweId",
                table: "JulietCoverages",
                column: "CweId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_CweId_RelatedCweID_Nature_ViewId_IsDerived",
                table: "Relationships",
                columns: new[] { "CweId", "RelatedCweID", "Nature", "ViewId", "IsDerived" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ScanId",
                table: "Reports",
                column: "ScanId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ScanId_GroundTruthCweId_ScannerCweId_RootCauseCweId",
                table: "Reports",
                columns: new[] { "ScanId", "GroundTruthCweId", "ScannerCweId", "RootCauseCweId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ScanId_ToolId",
                table: "Reports",
                columns: new[] { "ScanId", "ToolId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ToolId",
                table: "Reports",
                column: "ToolId");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_ToolId",
                table: "Scans",
                column: "ToolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CWECatalogs");

            migrationBuilder.DropTable(
                name: "CweSemanticRules");

            migrationBuilder.DropTable(
                name: "CWETestResults");

            migrationBuilder.DropTable(
                name: "JulietCoverages");

            migrationBuilder.DropTable(
                name: "Relationships");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "Scans");

            migrationBuilder.DropTable(
                name: "Tools");
        }
    }
}
