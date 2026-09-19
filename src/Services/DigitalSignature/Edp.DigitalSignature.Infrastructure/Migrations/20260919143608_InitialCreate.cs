using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edp.DigitalSignature.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProviderTransactions",
                columns: table => new
                {
                    ProviderTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SigningRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderRequestId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestType = table.Column<int>(type: "int", nullable: false),
                    RequestPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsePayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderTransactions", x => x.ProviderTransactionId);
                });

            migrationBuilder.CreateTable(
                name: "SignatureActions",
                columns: table => new
                {
                    SignatureActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SigningRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignatureData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignatureActions", x => x.SignatureActionId);
                });

            migrationBuilder.CreateTable(
                name: "SignatureFields",
                columns: table => new
                {
                    SignatureFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SigningRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentPage = table.Column<int>(type: "int", nullable: false),
                    FieldType = table.Column<int>(type: "int", nullable: false),
                    X = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Y = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Width = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Height = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignatureFields", x => x.SignatureFieldId);
                });

            migrationBuilder.CreateTable(
                name: "SigningRequests",
                columns: table => new
                {
                    SigningRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SigningMode = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DocumentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderRequestId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SigningRequests", x => x.SigningRequestId);
                });

            migrationBuilder.CreateTable(
                name: "Signers",
                columns: table => new
                {
                    SignerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SigningRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SigningOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    InvitedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeclinedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeclineReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderSignerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastReminderSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReminderCount = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signers", x => x.SignerId);
                    table.ForeignKey(
                        name: "FK_Signers_SigningRequests_SigningRequestId",
                        column: x => x.SigningRequestId,
                        principalTable: "SigningRequests",
                        principalColumn: "SigningRequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderTransactions_SigningRequestId_Provider_ProviderRequestId",
                table: "ProviderTransactions",
                columns: new[] { "SigningRequestId", "Provider", "ProviderRequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Signers_SigningRequestId",
                table: "Signers",
                column: "SigningRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningRequests_CreatedAt",
                table: "SigningRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SigningRequests_DocumentId",
                table: "SigningRequests",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningRequests_ExpiresAt",
                table: "SigningRequests",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_SigningRequests_OrganizationId",
                table: "SigningRequests",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SigningRequests_Status",
                table: "SigningRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SigningRequests_WorkflowInstanceId",
                table: "SigningRequests",
                column: "WorkflowInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderTransactions");

            migrationBuilder.DropTable(
                name: "SignatureActions");

            migrationBuilder.DropTable(
                name: "SignatureFields");

            migrationBuilder.DropTable(
                name: "Signers");

            migrationBuilder.DropTable(
                name: "SigningRequests");
        }
    }
}
