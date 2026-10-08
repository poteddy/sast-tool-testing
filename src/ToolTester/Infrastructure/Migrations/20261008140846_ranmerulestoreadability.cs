using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolTester.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ranmerulestoreadability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TargetCweId",
                table: "CweSemanticRules",
                newName: "TestTargetCwe");

            migrationBuilder.RenameColumn(
                name: "SourceCweId",
                table: "CweSemanticRules",
                newName: "ScannerFoundCweId");

            migrationBuilder.RenameIndex(
                name: "IX_CweSemanticRules_SourceCweId_TargetCweId_Relationship_ScannerRuleId_ProgrammingLanguage_Version",
                table: "CweSemanticRules",
                newName: "IX_CweSemanticRules_ScannerFoundCweId_TestTargetCwe_Relationship_ScannerRuleId_ProgrammingLanguage_Version");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TestTargetCwe",
                table: "CweSemanticRules",
                newName: "TargetCweId");

            migrationBuilder.RenameColumn(
                name: "ScannerFoundCweId",
                table: "CweSemanticRules",
                newName: "SourceCweId");

            migrationBuilder.RenameIndex(
                name: "IX_CweSemanticRules_ScannerFoundCweId_TestTargetCwe_Relationship_ScannerRuleId_ProgrammingLanguage_Version",
                table: "CweSemanticRules",
                newName: "IX_CweSemanticRules_SourceCweId_TargetCweId_Relationship_ScannerRuleId_ProgrammingLanguage_Version");
        }
    }
}
