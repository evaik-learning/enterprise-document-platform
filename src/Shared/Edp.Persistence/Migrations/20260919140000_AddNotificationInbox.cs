using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edp.Persistence.Migrations;

public partial class AddNotificationInbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "notification");

        migrationBuilder.CreateTable(
            name: "NotificationInboxMessages",
            schema: "notification",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                EventType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ProcessedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                Error = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_NotificationInboxMessages", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_NotificationInboxMessages_MessageId",
            schema: "notification",
            table: "NotificationInboxMessages",
            column: "MessageId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_NotificationInboxMessages_OrganizationId_ReceivedAtUtc",
            schema: "notification",
            table: "NotificationInboxMessages",
            columns: new[] { "OrganizationId", "ReceivedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "NotificationInboxMessages",
            schema: "notification");
    }
}
