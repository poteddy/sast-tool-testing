using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolTester.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addtooldeclaredcwes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ToolDeclaredCwes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ToolId = table.Column<int>(type: "INTEGER", nullable: false),
                    CweId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolDeclaredCwes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolDeclaredCwes_CWECatalogs_CweId",
                        column: x => x.CweId,
                        principalTable: "CWECatalogs",
                        principalColumn: "CweId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ToolDeclaredCwes_Tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "Tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ToolDeclaredCwes_CweId",
                table: "ToolDeclaredCwes",
                column: "CweId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolDeclaredCwes_ToolId_CweId",
                table: "ToolDeclaredCwes",
                columns: new[] { "ToolId", "CweId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ToolDeclaredCwes");
        }
    }
}
