using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompanionBrainSpine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskItems_UserProfileId_Status",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_MemoryEntries_UserProfileId_Type",
                table: "MemoryEntries");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_UserProfileId_CreatedUtc",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_Status_CreatedUtc",
                table: "ApprovalRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedUtc",
                table: "TaskItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceMessageId",
                table: "TaskItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "Messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokensEstimate",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresUtc",
                table: "MemoryEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Importance",
                table: "MemoryEntries",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "MemoryEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Sensitivity",
                table: "MemoryEntries",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.AddColumn<string>(
                name: "ActiveTopic",
                table: "Conversations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMessageUtc",
                table: "Conversations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "ApprovalRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskLevel",
                table: "ApprovalRequests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Medium");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceMessageId",
                table: "ApprovalRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserProfileId",
                table: "ApprovalRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "AgentRuns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "AgentRuns",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "AgentRuns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserProfileId",
                table: "AgentRuns",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Conversations"
                SET "LastMessageUtc" = "UpdatedUtc",
                    "ActiveTopic" = COALESCE("ActiveTopic", "Title");
                """);

            migrationBuilder.UpdateData(
                table: "Conversations",
                keyColumn: "Id",
                keyValue: new Guid("7d5359f4-09b2-4351-9c8d-c4c34b19d74f"),
                columns: new[] { "ActiveTopic", "LastMessageUtc" },
                values: new object[] { "Companion Core Onboarding", new DateTime(2026, 6, 19, 12, 5, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "MemoryEntries",
                keyColumn: "Id",
                keyValue: new Guid("4f761be7-f7c7-4bd3-bff5-6f3aa08e09aa"),
                columns: new[] { "ExpiresUtc", "Importance", "Sensitivity" },
                values: new object[] { null, 4, "Normal" });

            migrationBuilder.UpdateData(
                table: "MemoryEntries",
                keyColumn: "Id",
                keyValue: new Guid("de687616-b0c7-4715-9a31-c1e1fc792532"),
                columns: new[] { "ExpiresUtc", "Importance", "Sensitivity" },
                values: new object[] { null, 3, "Normal" });

            migrationBuilder.UpdateData(
                table: "MemoryEntries",
                keyColumn: "Id",
                keyValue: new Guid("e550806f-384c-4104-84c9-f5cff8297900"),
                columns: new[] { "ExpiresUtc", "Importance", "Sensitivity" },
                values: new object[] { null, 5, "Normal" });

            migrationBuilder.UpdateData(
                table: "TaskItems",
                keyColumn: "Id",
                keyValue: new Guid("0ec7d330-08fb-44fe-9b34-e0075ef75f8a"),
                columns: new[] { "CompletedUtc", "SourceMessageId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TaskItems",
                keyColumn: "Id",
                keyValue: new Guid("9995cd58-1841-4732-8c87-34a6334f6652"),
                columns: new[] { "CompletedUtc", "SourceMessageId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "TaskItems",
                keyColumn: "Id",
                keyValue: new Guid("b7fd0e94-87ba-42ed-a0f7-f8ce8b2d4cc4"),
                columns: new[] { "CompletedUtc", "SourceMessageId" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_SourceMessageId",
                table: "TaskItems",
                column: "SourceMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_UserProfileId_Status_DueDateUtc",
                table: "TaskItems",
                columns: new[] { "UserProfileId", "Status", "DueDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryEntries_UserProfileId_IsArchived_Type",
                table: "MemoryEntries",
                columns: new[] { "UserProfileId", "IsArchived", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserProfileId_LastMessageUtc",
                table: "Conversations",
                columns: new[] { "UserProfileId", "LastMessageUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_ConversationId",
                table: "ApprovalRequests",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_SourceMessageId",
                table: "ApprovalRequests",
                column: "SourceMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_UserProfileId_Status_CreatedUtc",
                table: "ApprovalRequests",
                columns: new[] { "UserProfileId", "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_ConversationId",
                table: "AgentRuns",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_UserProfileId",
                table: "AgentRuns",
                column: "UserProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentRuns_Conversations_ConversationId",
                table: "AgentRuns",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentRuns_UserProfiles_UserProfileId",
                table: "AgentRuns",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_Conversations_ConversationId",
                table: "ApprovalRequests",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_Messages_SourceMessageId",
                table: "ApprovalRequests",
                column: "SourceMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRequests_UserProfiles_UserProfileId",
                table: "ApprovalRequests",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_Messages_SourceMessageId",
                table: "TaskItems",
                column: "SourceMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentRuns_Conversations_ConversationId",
                table: "AgentRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentRuns_UserProfiles_UserProfileId",
                table: "AgentRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequests_Conversations_ConversationId",
                table: "ApprovalRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequests_Messages_SourceMessageId",
                table: "ApprovalRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRequests_UserProfiles_UserProfileId",
                table: "ApprovalRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_Messages_SourceMessageId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_SourceMessageId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_UserProfileId_Status_DueDateUtc",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_MemoryEntries_UserProfileId_IsArchived_Type",
                table: "MemoryEntries");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_UserProfileId_LastMessageUtc",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_ConversationId",
                table: "ApprovalRequests");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_SourceMessageId",
                table: "ApprovalRequests");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalRequests_UserProfileId_Status_CreatedUtc",
                table: "ApprovalRequests");

            migrationBuilder.DropIndex(
                name: "IX_AgentRuns_ConversationId",
                table: "AgentRuns");

            migrationBuilder.DropIndex(
                name: "IX_AgentRuns_UserProfileId",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "CompletedUtc",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "SourceMessageId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "TokensEstimate",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ExpiresUtc",
                table: "MemoryEntries");

            migrationBuilder.DropColumn(
                name: "Importance",
                table: "MemoryEntries");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "MemoryEntries");

            migrationBuilder.DropColumn(
                name: "Sensitivity",
                table: "MemoryEntries");

            migrationBuilder.DropColumn(
                name: "ActiveTopic",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "LastMessageUtc",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "ApprovalRequests");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "ApprovalRequests");

            migrationBuilder.DropColumn(
                name: "SourceMessageId",
                table: "ApprovalRequests");

            migrationBuilder.DropColumn(
                name: "UserProfileId",
                table: "ApprovalRequests");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "Error",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "UserProfileId",
                table: "AgentRuns");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_UserProfileId_Status",
                table: "TaskItems",
                columns: new[] { "UserProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryEntries_UserProfileId_Type",
                table: "MemoryEntries",
                columns: new[] { "UserProfileId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserProfileId_CreatedUtc",
                table: "Conversations",
                columns: new[] { "UserProfileId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_Status_CreatedUtc",
                table: "ApprovalRequests",
                columns: new[] { "Status", "CreatedUtc" });
        }
    }
}
