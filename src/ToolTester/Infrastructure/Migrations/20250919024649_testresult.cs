using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolTester.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class testresult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CWETestResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Test = table.Column<int>(type: "INTEGER", nullable: false),
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
                    Cwe = table.Column<int>(type: "INTEGER", nullable: false),
                    Mitigation = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CWETestResults", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CWETestResults");
        }
    }
}
