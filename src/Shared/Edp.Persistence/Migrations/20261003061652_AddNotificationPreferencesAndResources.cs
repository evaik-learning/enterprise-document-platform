using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferencesAndResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignmentRulesJson",
                table: "WorkflowStates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResourceId",
                schema: "notification",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceType",
                schema: "notification",
                table: "Notifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                schema: "notification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "bit", nullable: false),
                    WorkflowEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SignatureEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AuditEnabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_OrganizationId_UserId",
                schema: "notification",
                table: "NotificationPreferences",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationPreferences",
                schema: "notification");

            migrationBuilder.DropColumn(
                name: "AssignmentRulesJson",
                table: "WorkflowStates");

            migrationBuilder.DropColumn(
                name: "ResourceId",
                schema: "notification",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ResourceType",
                schema: "notification",
                table: "Notifications");
        }
    }
}
