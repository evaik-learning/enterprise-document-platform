using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edp.Persistence.Migrations;

public partial class AddWorkflowOutboxStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Status",
            schema: "workflow",
            table: "OutboxMessages",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "Pending");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Status",
            schema: "workflow",
            table: "OutboxMessages");
    }
}