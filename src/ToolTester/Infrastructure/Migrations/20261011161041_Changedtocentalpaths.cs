using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolTester.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Changedtocentalpaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Test",
                table: "CWETestResults",
                newName: "TestId");

            migrationBuilder.RenameIndex(
                name: "IX_CWETestResults_ScanId_Test",
                table: "CWETestResults",
                newName: "IX_CWETestResults_ScanId_TestId");

            migrationBuilder.AddColumn<int>(
                name: "RootCauseCweId",
                table: "CWETestResults",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JulietTests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NormalizedPath = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    CweId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JulietTests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CWETestResults_TestId",
                table: "CWETestResults",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_CWETestResults_TestId_ScannerFoundCWE",
                table: "CWETestResults",
                columns: new[] { "TestId", "ScannerFoundCWE" });

            migrationBuilder.CreateIndex(
                name: "IX_JulietTests_CweId",
                table: "JulietTests",
                column: "CweId");

            migrationBuilder.CreateIndex(
                name: "IX_JulietTests_NormalizedPath",
                table: "JulietTests",
                column: "NormalizedPath",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CWETestResults_JulietTests_TestId",
                table: "CWETestResults",
                column: "TestId",
                principalTable: "JulietTests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CWETestResults_JulietTests_TestId",
                table: "CWETestResults");

            migrationBuilder.DropTable(
                name: "JulietTests");

            migrationBuilder.DropIndex(
                name: "IX_CWETestResults_TestId",
                table: "CWETestResults");

            migrationBuilder.DropIndex(
                name: "IX_CWETestResults_TestId_ScannerFoundCWE",
                table: "CWETestResults");

            migrationBuilder.DropColumn(
                name: "RootCauseCweId",
                table: "CWETestResults");

            migrationBuilder.RenameColumn(
                name: "TestId",
                table: "CWETestResults",
                newName: "Test");

            migrationBuilder.RenameIndex(
                name: "IX_CWETestResults_ScanId_TestId",
                table: "CWETestResults",
                newName: "IX_CWETestResults_ScanId_Test");
        }
    }
}
