using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace calledudeBotCore.Migrations;

public partial class UserActivity_MessagesSent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "MessagesSent",
            table: "UserActivities",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MessagesSent",
            table: "UserActivities");
    }
}
