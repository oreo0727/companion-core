using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Input = table.Column<string>(type: "text", nullable: false),
                    Output = table.Column<string>(type: "text", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConnectorAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversations_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemoryEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastReferencedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemoryEntries_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskItems_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "UserProfiles",
                columns: new[] { "Id", "CreatedUtc", "DisplayName", "Email", "UpdatedUtc" },
                values: new object[] { new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f"), new DateTime(2026, 6, 19, 12, 0, 0, 0, DateTimeKind.Utc), "Local User", "local.user@companion-core.local", new DateTime(2026, 6, 19, 12, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Conversations",
                columns: new[] { "Id", "CreatedUtc", "Title", "UpdatedUtc", "UserProfileId" },
                values: new object[] { new Guid("7d5359f4-09b2-4351-9c8d-c4c34b19d74f"), new DateTime(2026, 6, 19, 12, 5, 0, 0, DateTimeKind.Utc), "Companion Core Onboarding", new DateTime(2026, 6, 19, 12, 5, 0, 0, DateTimeKind.Utc), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") });

            migrationBuilder.InsertData(
                table: "MemoryEntries",
                columns: new[] { "Id", "Confidence", "Content", "CreatedUtc", "LastReferencedUtc", "Source", "Summary", "Type", "UserProfileId" },
                values: new object[,]
                {
                    { new Guid("4f761be7-f7c7-4bd3-bff5-6f3aa08e09aa"), 0.93m, "The local user prefers direct, high-signal summaries during development work.", new DateTime(2026, 6, 19, 12, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 19, 12, 10, 0, 0, DateTimeKind.Utc), "Seed", "Prefers concise status updates", "Preference", new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("de687616-b0c7-4715-9a31-c1e1fc792532"), 0.88m, "The local user responds well to supportive, practical progress updates while building software.", new DateTime(2026, 6, 19, 12, 12, 0, 0, DateTimeKind.Utc), null, "Seed", "Likes collaborative momentum", "Style", new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("e550806f-384c-4104-84c9-f5cff8297900"), 0.98m, "Companion Core is the backend foundation for a private AI companion platform.", new DateTime(2026, 6, 19, 12, 11, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 19, 12, 11, 0, 0, DateTimeKind.Utc), "Seed", "Building Companion Core", "Project", new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") }
                });

            migrationBuilder.InsertData(
                table: "TaskItems",
                columns: new[] { "Id", "CreatedUtc", "Description", "DueDateUtc", "Priority", "Status", "Title", "UserProfileId" },
                values: new object[,]
                {
                    { new Guid("0ec7d330-08fb-44fe-9b34-e0075ef75f8a"), new DateTime(2026, 6, 19, 12, 17, 0, 0, DateTimeKind.Utc), "Queue a pending agent run and confirm the worker moves it to completed.", new DateTime(2026, 6, 22, 17, 0, 0, 0, DateTimeKind.Utc), "High", "Todo", "Observe worker-driven agent runs", new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("9995cd58-1841-4732-8c87-34a6334f6652"), new DateTime(2026, 6, 19, 12, 16, 0, 0, DateTimeKind.Utc), "Create a sample approval request and verify it can be approved or rejected.", new DateTime(2026, 6, 21, 17, 0, 0, 0, DateTimeKind.Utc), "Normal", "InProgress", "Exercise the approval workflow", new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("b7fd0e94-87ba-42ed-a0f7-f8ce8b2d4cc4"), new DateTime(2026, 6, 19, 12, 15, 0, 0, DateTimeKind.Utc), "Confirm the core endpoints match the first Companion Core backend milestone.", new DateTime(2026, 6, 20, 17, 0, 0, 0, DateTimeKind.Utc), "High", "Todo", "Review the initial API surface", new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_Status_CreatedUtc",
                table: "AgentRuns",
                columns: new[] { "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_Status_CreatedUtc",
                table: "ApprovalRequests",
                columns: new[] { "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorAccounts_Provider_Status",
                table: "ConnectorAccounts",
                columns: new[] { "Provider", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserProfileId_CreatedUtc",
                table: "Conversations",
                columns: new[] { "UserProfileId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryEntries_UserProfileId_Type",
                table: "MemoryEntries",
                columns: new[] { "UserProfileId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_CreatedUtc",
                table: "Messages",
                columns: new[] { "ConversationId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_UserProfileId_Status",
                table: "TaskItems",
                columns: new[] { "UserProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentRuns");

            migrationBuilder.DropTable(
                name: "ApprovalRequests");

            migrationBuilder.DropTable(
                name: "ConnectorAccounts");

            migrationBuilder.DropTable(
                name: "MemoryEntries");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "TaskItems");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}
