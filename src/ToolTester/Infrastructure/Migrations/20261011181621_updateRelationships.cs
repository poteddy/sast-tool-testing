using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolTester.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TestPathListedCWE",
                table: "CWETestResults");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CWECatalogs_CweId",
                table: "CWECatalogs",
                column: "CweId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_RelatedCweID",
                table: "Relationships",
                column: "RelatedCweID");

            migrationBuilder.AddForeignKey(
                name: "FK_JulietCoverages_CWECatalogs_CweId",
                table: "JulietCoverages",
                column: "CweId",
                principalTable: "CWECatalogs",
                principalColumn: "CweId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JulietTests_CWECatalogs_CweId",
                table: "JulietTests",
                column: "CweId",
                principalTable: "CWECatalogs",
                principalColumn: "CweId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Relationships_CWECatalogs_CweId",
                table: "Relationships",
                column: "CweId",
                principalTable: "CWECatalogs",
                principalColumn: "CweId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Relationships_CWECatalogs_RelatedCweID",
                table: "Relationships",
                column: "RelatedCweID",
                principalTable: "CWECatalogs",
                principalColumn: "CweId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JulietCoverages_CWECatalogs_CweId",
                table: "JulietCoverages");

            migrationBuilder.DropForeignKey(
                name: "FK_JulietTests_CWECatalogs_CweId",
                table: "JulietTests");

            migrationBuilder.DropForeignKey(
                name: "FK_Relationships_CWECatalogs_CweId",
                table: "Relationships");

            migrationBuilder.DropForeignKey(
                name: "FK_Relationships_CWECatalogs_RelatedCweID",
                table: "Relationships");

            migrationBuilder.DropIndex(
                name: "IX_Relationships_RelatedCweID",
                table: "Relationships");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CWECatalogs_CweId",
                table: "CWECatalogs");

            migrationBuilder.AddColumn<int>(
                name: "TestPathListedCWE",
                table: "CWETestResults",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
