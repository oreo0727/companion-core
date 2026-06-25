using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase6AgentToolRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ToolDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToolExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    InputJson = table.Column<string>(type: "text", nullable: false),
                    OutputJson = table.Column<string>(type: "text", nullable: true),
                    Error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    StartedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolExecutions_AgentRuns_AgentRunId",
                        column: x => x.AgentRunId,
                        principalTable: "AgentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ToolExecutions_ToolDefinitions_ToolDefinitionId",
                        column: x => x.ToolDefinitionId,
                        principalTable: "ToolDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ToolExecutions_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToolPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Allowed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolPermissions_ToolDefinitions_ToolDefinitionId",
                        column: x => x.ToolDefinitionId,
                        principalTable: "ToolDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ToolPermissions_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ToolDefinitions",
                columns: new[] { "Id", "Category", "CreatedUtc", "Description", "Enabled", "Name", "RequiresApproval", "RiskLevel" },
                values: new object[,]
                {
                    { new Guid("56ec1a59-1115-4da9-9292-c8a2609fe632"), "Companion", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Retrieve the authenticated user's current companion briefing.", true, "GetBriefing", false, "Low" },
                    { new Guid("ba1ae420-3338-40cb-b7be-b7a08b95fe7b"), "Memory", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Search the authenticated user's saved memories.", true, "MemorySearch", false, "Low" },
                    { new Guid("d39d98cc-c066-44b5-bc05-6dc81c7dbf6c"), "Planning", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Create a task for the authenticated user after approval.", true, "CreateTask", true, "Medium" }
                });

            migrationBuilder.InsertData(
                table: "ToolPermissions",
                columns: new[] { "Id", "Allowed", "CreatedUtc", "ToolDefinitionId", "UserProfileId" },
                values: new object[,]
                {
                    { new Guid("1a9f7783-8d03-4769-ab39-f9b8dc7bd3b4"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("ba1ae420-3338-40cb-b7be-b7a08b95fe7b"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("b4608125-c91c-4a2a-ae17-68a4b0f4f6df"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("d39d98cc-c066-44b5-bc05-6dc81c7dbf6c"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("f2a6cdb9-212d-4f0f-92a1-0e2db84cf90f"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("56ec1a59-1115-4da9-9292-c8a2609fe632"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ToolDefinitions_Name",
                table: "ToolDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolExecutions_AgentRunId",
                table: "ToolExecutions",
                column: "AgentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolExecutions_ToolDefinitionId_Status",
                table: "ToolExecutions",
                columns: new[] { "ToolDefinitionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ToolExecutions_UserProfileId_StartedUtc",
                table: "ToolExecutions",
                columns: new[] { "UserProfileId", "StartedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ToolPermissions_ToolDefinitionId",
                table: "ToolPermissions",
                column: "ToolDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolPermissions_UserProfileId_ToolDefinitionId",
                table: "ToolPermissions",
                columns: new[] { "UserProfileId", "ToolDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ToolExecutions");

            migrationBuilder.DropTable(
                name: "ToolPermissions");

            migrationBuilder.DropTable(
                name: "ToolDefinitions");
        }
    }
}
