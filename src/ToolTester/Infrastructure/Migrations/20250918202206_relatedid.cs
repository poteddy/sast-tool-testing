using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToolTester.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class relatedid : Migration
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
                name: "RelationsShips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CWEID = table.Column<int>(type: "INTEGER", nullable: false),
                    RelatedCweID = table.Column<int>(type: "INTEGER", nullable: false),
                    Nature = table.Column<string>(type: "TEXT", nullable: false),
                    Oridinal = table.Column<string>(type: "TEXT", nullable: false),
                    OrderSpecified = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChainId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelationsShips", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CWECatalogs");

            migrationBuilder.DropTable(
                name: "RelationsShips");
        }
    }
}
