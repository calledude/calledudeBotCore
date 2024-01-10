using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace calledudeBotCore.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UserActivities",
            columns: table => new
            {
                Username = table.Column<string>(nullable: false),
                TimesSeen = table.Column<int>(nullable: false),
                LastJoinDate = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_UserActivities", x => x.Username));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UserActivities");
    }
}
