using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace calledudeBotCore.Migrations;

public partial class StreamSession_UserSession : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<Guid>(
			name: "StreamSession",
			table: "UserActivities",
			type: "TEXT",
			nullable: false,
			defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropColumn(
			name: "StreamSession",
			table: "UserActivities");
	}
}