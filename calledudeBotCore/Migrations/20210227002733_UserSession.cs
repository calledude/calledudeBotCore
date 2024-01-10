using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace calledudeBotCore.Migrations;

public partial class UserSession : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable(
			name: "UserSession",
			columns: table => new
			{
				Username = table.Column<string>(type: "TEXT", nullable: false),
				WatchTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
				StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
				EndTime = table.Column<DateTime>(type: "TEXT", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_UserSession", x => x.Username);
			});
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable(
			name: "UserSession");
	}
}