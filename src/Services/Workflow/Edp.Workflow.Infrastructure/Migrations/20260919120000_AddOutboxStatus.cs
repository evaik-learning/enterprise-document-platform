using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edp.Workflow.Infrastructure.Migrations;

public partial class AddOutboxStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Status",
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
            table: "OutboxMessages");
    }
}