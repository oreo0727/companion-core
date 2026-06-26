using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase18MultiAgentOrchestration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentRuns_UserProfileId",
                table: "AgentRuns");

            migrationBuilder.AddColumn<Guid>(
                name: "AgentDefinitionId",
                table: "AgentRuns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DelegationReason",
                table: "AgentRuns",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentAgentRunId",
                table: "AgentRuns",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Prompt = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ToolNamesJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ContextPolicyJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    MemoryWeight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDefinitions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AgentDefinitions",
                columns: new[] { "Id", "ContextPolicyJson", "CreatedUtc", "Description", "DisplayName", "Enabled", "MemoryWeight", "Name", "Prompt", "ToolNamesJson" },
                values: new object[,]
                {
                    { new Guid("1f4225ea-6c77-4115-9591-a03e9a747a47"), "{\"conversation\":0.9,\"memories\":0.8,\"goals\":0.5,\"projects\":0.5,\"knowledge\":0.5,\"tasks\":0.7,\"calendar\":1.0,\"email\":0.5,\"home\":0.1}", new DateTime(2026, 6, 26, 22, 0, 0, 0, DateTimeKind.Utc), "Coordinates trip planning context without booking or external connector actions.", "Travel", true, 0.65m, "Travel", "Organize travel intent, constraints, itinerary questions, and follow-up tasks.", "[\"MemorySearch\",\"CreateTask\",\"CalendarEvents\"]" },
                    { new Guid("417bd4a0-806e-4d44-a64d-d6cc665a9273"), "{\"conversation\":0.8,\"memories\":0.9,\"goals\":0.6,\"projects\":0.4,\"knowledge\":0.5,\"tasks\":0.8,\"calendar\":0.4,\"email\":1.0,\"home\":0.1}", new DateTime(2026, 6, 26, 22, 0, 0, 0, DateTimeKind.Utc), "Tracks bills, payment language, budget-related tasks, and finance follow-ups.", "Finance", true, 0.90m, "Finance", "Identify financial obligations and risks without initiating transactions.", "[\"EmailSearch\",\"MemorySearch\",\"CreateTask\",\"CreateReminder\"]" },
                    { new Guid("550293e9-eeaa-4afa-b187-932696c4a8af"), "{\"conversation\":1.0,\"memories\":0.8,\"goals\":0.5,\"projects\":0.6,\"knowledge\":0.8,\"tasks\":0.3,\"calendar\":0.2,\"email\":0.6,\"home\":0.1}", new DateTime(2026, 6, 26, 22, 0, 0, 0, DateTimeKind.Utc), "Drafts, summarizes, edits, and turns scattered context into usable prose.", "Writer", true, 0.70m, "Writer", "Write clearly in the user's voice while preserving facts and open questions.", "[\"KnowledgeSearch\",\"MemorySearch\"]" },
                    { new Guid("6d7d47db-d40f-4f77-af0c-971b49313df6"), "{\"conversation\":0.8,\"memories\":0.8,\"goals\":1.0,\"projects\":1.0,\"knowledge\":0.5,\"tasks\":1.0,\"calendar\":0.9,\"email\":0.5,\"home\":0.3}", new DateTime(2026, 6, 26, 22, 0, 0, 0, DateTimeKind.Utc), "Turns goals, projects, open loops, reminders, and tasks into practical plans.", "Planner", true, 0.85m, "Planner", "Plan next actions, reduce ambiguity, and surface sequencing tradeoffs.", "[\"GetBriefing\",\"CreateTask\",\"CreateReminder\",\"ListNotifications\"]" },
                    { new Guid("7cfa4ef1-d530-47a9-b208-fc71f6cc7ec8"), "{\"conversation\":0.8,\"memories\":0.7,\"goals\":0.6,\"projects\":0.9,\"knowledge\":1.0,\"tasks\":0.8,\"calendar\":0.2,\"email\":0.2,\"home\":0.1}", new DateTime(2026, 6, 26, 22, 0, 0, 0, DateTimeKind.Utc), "Reasons about implementation tasks, technical risks, tests, and delivery plans.", "Coder", true, 0.75m, "Coder", "Think like a careful software engineer: inspect context, propose concrete changes, and protect tests.", "[\"KnowledgeSearch\",\"MemorySearch\",\"CreateTask\"]" },
                    { new Guid("a5a1be19-9437-4558-8f4f-a3f5e5de78d6"), "{\"conversation\":0.8,\"memories\":1.0,\"goals\":0.6,\"projects\":0.3,\"knowledge\":0.4,\"tasks\":0.8,\"calendar\":0.9,\"email\":0.3,\"home\":0.2}", new DateTime(2026, 6, 26, 22, 0, 0, 0, DateTimeKind.Utc), "Organizes health-related reminders and context while avoiding medical diagnosis.", "Health", true, 1.00m, "Health", "Help track health routines, appointments, and questions for professionals.", "[\"MemorySearch\",\"CreateTask\",\"CreateReminder\",\"CalendarEvents\"]" },
                    { new Guid("aa5b274e-8b37-41e3-a366-74939af84513"), "{\"conversation\":0.7,\"memories\":0.5,\"goals\":0.3,\"projects\":0.3,\"knowledge\":0.3,\"tasks\":0.5,\"calendar\":0.6,\"email\":0.2,\"home\":1.0}", new DateTime(2026, 6, 26, 22, 0, 0, 0, DateTimeKind.Utc), "Understands home device and sensor state and routes risky actions through approvals.", "Home", true, 0.60m, "Home", "Read home state, explain device context, and never bypass action approval.", "[\"HomeStatus\",\"HomeExecuteAction\",\"CalendarEvents\",\"CreateReminder\"]" },
                    { new Guid("c332bd8d-17db-4837-a413-0d079d72ebdf"), "{\"conversation\":0.7,\"memories\":0.9,\"goals\":0.5,\"projects\":0.6,\"knowledge\":1.0,\"tasks\":0.3,\"calendar\":0.2,\"email\":0.4,\"home\":0.1}", new DateTime(2026, 6, 26, 22, 0, 0, 0, DateTimeKind.Utc), "Finds and compares relevant knowledge from internal documents and memories.", "Research", true, 0.95m, "Research", "Ground answers in stored knowledge and clearly separate evidence from inference.", "[\"KnowledgeSearch\",\"MemorySearch\"]" },
                    { new Guid("c94f493e-d977-47a5-a80c-6425b5d64c38"), "{\"conversation\":1.0,\"memories\":1.0,\"goals\":1.0,\"projects\":1.0,\"knowledge\":0.8,\"tasks\":1.0,\"calendar\":1.0,\"email\":0.9,\"home\":0.7}", new DateTime(2026, 6, 26, 22, 0, 0, 0, DateTimeKind.Utc), "Coordinates priorities, delegates to specialists, and keeps the overall operating picture coherent.", "Chief of Staff", true, 1.00m, "ChiefOfStaff", "Coordinate the user's work, delegate to specialists when useful, and keep every action explainable.", "[\"GetBriefing\",\"MemorySearch\",\"KnowledgeSearch\",\"CalendarEvents\",\"EmailSearch\",\"ListNotifications\"]" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_AgentDefinitionId",
                table: "AgentRuns",
                column: "AgentDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_ParentAgentRunId",
                table: "AgentRuns",
                column: "ParentAgentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_UserProfileId_AgentName_CreatedUtc",
                table: "AgentRuns",
                columns: new[] { "UserProfileId", "AgentName", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinitions_Name",
                table: "AgentDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentRuns_AgentDefinitions_AgentDefinitionId",
                table: "AgentRuns",
                column: "AgentDefinitionId",
                principalTable: "AgentDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentRuns_AgentRuns_ParentAgentRunId",
                table: "AgentRuns",
                column: "ParentAgentRunId",
                principalTable: "AgentRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentRuns_AgentDefinitions_AgentDefinitionId",
                table: "AgentRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentRuns_AgentRuns_ParentAgentRunId",
                table: "AgentRuns");

            migrationBuilder.DropTable(
                name: "AgentDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_AgentRuns_AgentDefinitionId",
                table: "AgentRuns");

            migrationBuilder.DropIndex(
                name: "IX_AgentRuns_ParentAgentRunId",
                table: "AgentRuns");

            migrationBuilder.DropIndex(
                name: "IX_AgentRuns_UserProfileId_AgentName_CreatedUtc",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "AgentDefinitionId",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "DelegationReason",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "ParentAgentRunId",
                table: "AgentRuns");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_UserProfileId",
                table: "AgentRuns",
                column: "UserProfileId");
        }
    }
}
