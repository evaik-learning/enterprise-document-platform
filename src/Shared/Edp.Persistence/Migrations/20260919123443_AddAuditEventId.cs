using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edp.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEventId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE [AuditLogs] SET [EventId] = NEWID() WHERE [EventId] = '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OrganizationId_EventId",
                table: "AuditLogs",
                columns: new[] { "OrganizationId", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_OrganizationId_EventId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "AuditLogs");
        }
    }
}
