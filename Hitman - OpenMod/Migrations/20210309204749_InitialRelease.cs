using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Hitman.Migrations
{
    public partial class InitialRelease : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hits",
                columns: table => new
                {
                    HitId = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TargetPlayerId = table.Column<string>(nullable: false),
                    HirerPlayerId = table.Column<string>(nullable: true),
                    Bounty = table.Column<decimal>(nullable: false),
                    TimePlaced = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hits", x => x.HitId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hits");
        }
    }
}
