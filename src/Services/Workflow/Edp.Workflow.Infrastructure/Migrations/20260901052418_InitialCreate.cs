using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edp.Workflow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersion = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentStateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InitiatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LatestVersion = table.Column<int>(type: "int", nullable: false),
                    PublishedVersion = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeadlineAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ReassignedFromUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    WorkflowInstanceId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalTasks_WorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalTasks_WorkflowInstances_WorkflowInstanceId1",
                        column: x => x.WorkflowInstanceId1,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowHistory_WorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStateInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExitedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WorkflowInstanceId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStateInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStateInstances_WorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowStateInstances_WorkflowInstances_WorkflowInstanceId1",
                        column: x => x.WorkflowInstanceId1,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowVariables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false),
                    WorkflowInstanceId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowVariables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowVariables_WorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowVariables_WorkflowInstances_WorkflowInstanceId1",
                        column: x => x.WorkflowInstanceId1,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    StartStateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowVersions_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowVersions_Workflows_WorkflowId1",
                        column: x => x.WorkflowId1,
                        principalTable: "Workflows",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActionByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActionAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalTaskId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalActions_ApprovalTasks_ApprovalTaskId",
                        column: x => x.ApprovalTaskId,
                        principalTable: "ApprovalTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalActions_ApprovalTasks_ApprovalTaskId1",
                        column: x => x.ApprovalTaskId1,
                        principalTable: "ApprovalTasks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StateType = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Configuration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalPolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkflowVersionId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStates_WorkflowVersions_WorkflowVersionId",
                        column: x => x.WorkflowVersionId,
                        principalTable: "WorkflowVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowStates_WorkflowVersions_WorkflowVersionId1",
                        column: x => x.WorkflowVersionId1,
                        principalTable: "WorkflowVersions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToStateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuardJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkflowVersionId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_WorkflowVersions_WorkflowVersionId",
                        column: x => x.WorkflowVersionId,
                        principalTable: "WorkflowVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_WorkflowVersions_WorkflowVersionId1",
                        column: x => x.WorkflowVersionId1,
                        principalTable: "WorkflowVersions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalActions_ApprovalTaskId",
                table: "ApprovalActions",
                column: "ApprovalTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalActions_ApprovalTaskId1",
                table: "ApprovalActions",
                column: "ApprovalTaskId1");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalTasks_OrganizationId_AssignedToUserId_Status",
                table: "ApprovalTasks",
                columns: new[] { "OrganizationId", "AssignedToUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalTasks_WorkflowInstanceId",
                table: "ApprovalTasks",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalTasks_WorkflowInstanceId1",
                table: "ApprovalTasks",
                column: "WorkflowInstanceId1");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowOutboxMessages_Pending",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOnUtc", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowHistory_OrganizationId_WorkflowInstanceId_EventAt",
                table: "WorkflowHistory",
                columns: new[] { "OrganizationId", "WorkflowInstanceId", "EventAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowHistory_WorkflowInstanceId",
                table: "WorkflowHistory",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_OrganizationId_DocumentId",
                table: "WorkflowInstances",
                columns: new[] { "OrganizationId", "DocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_OrganizationId_Status",
                table: "WorkflowInstances",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_OrganizationId",
                table: "Workflows",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_OrganizationId_Code",
                table: "Workflows",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStateInstances_WorkflowInstanceId",
                table: "WorkflowStateInstances",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStateInstances_WorkflowInstanceId1",
                table: "WorkflowStateInstances",
                column: "WorkflowInstanceId1");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStates_WorkflowVersionId_Name",
                table: "WorkflowStates",
                columns: new[] { "WorkflowVersionId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStates_WorkflowVersionId1",
                table: "WorkflowStates",
                column: "WorkflowVersionId1");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_WorkflowVersionId_FromStateId_Order",
                table: "WorkflowTransitions",
                columns: new[] { "WorkflowVersionId", "FromStateId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_WorkflowVersionId1",
                table: "WorkflowTransitions",
                column: "WorkflowVersionId1");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVariables_WorkflowInstanceId_Name",
                table: "WorkflowVariables",
                columns: new[] { "WorkflowInstanceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVariables_WorkflowInstanceId1",
                table: "WorkflowVariables",
                column: "WorkflowInstanceId1");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersions_WorkflowId_Version",
                table: "WorkflowVersions",
                columns: new[] { "WorkflowId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersions_WorkflowId1",
                table: "WorkflowVersions",
                column: "WorkflowId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalActions");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "WorkflowHistory");

            migrationBuilder.DropTable(
                name: "WorkflowStateInstances");

            migrationBuilder.DropTable(
                name: "WorkflowStates");

            migrationBuilder.DropTable(
                name: "WorkflowTransitions");

            migrationBuilder.DropTable(
                name: "WorkflowVariables");

            migrationBuilder.DropTable(
                name: "ApprovalTasks");

            migrationBuilder.DropTable(
                name: "WorkflowVersions");

            migrationBuilder.DropTable(
                name: "WorkflowInstances");

            migrationBuilder.DropTable(
                name: "Workflows");
        }
    }
}
