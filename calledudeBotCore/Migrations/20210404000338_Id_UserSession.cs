using Microsoft.EntityFrameworkCore.Migrations;

namespace calledudeBotCore.Migrations;

public partial class Id_UserSession : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_UserSession",
            table: "UserSession");

        migrationBuilder.AddColumn<int>(
            name: "Id",
            table: "UserSession",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0)
            .Annotation("Sqlite:Autoincrement", true);

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserSession",
            table: "UserSession",
            column: "Id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_UserSession",
            table: "UserSession");

        migrationBuilder.DropColumn(
            name: "Id",
            table: "UserSession");

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserSession",
            table: "UserSession",
            column: "Username");
    }
}
