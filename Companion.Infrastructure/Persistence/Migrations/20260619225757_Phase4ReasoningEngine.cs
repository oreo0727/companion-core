using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4ReasoningEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletionTokens",
                table: "AgentRuns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LatencyMs",
                table: "AgentRuns",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "AgentRuns",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromptTokens",
                table: "AgentRuns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "AgentRuns",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApiBaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ApiKeyEncrypted = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Temperature = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiProviderConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemorySuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Importance = table.Column<int>(type: "integer", nullable: false),
                    Sensitivity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemorySuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemorySuggestions_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskSuggestions_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AiProviderConfigurations",
                columns: new[] { "Id", "ApiBaseUrl", "ApiKeyEncrypted", "CreatedUtc", "IsEnabled", "MaxTokens", "Model", "Provider", "Temperature", "UpdatedUtc" },
                values: new object[,]
                {
                    { new Guid("2d9e33d7-4386-4d20-8d2d-68ccdb554a7d"), "https://api.anthropic.com/v1", "", new DateTime(2026, 6, 19, 12, 30, 0, 0, DateTimeKind.Utc), false, 600, "claude-3-5-sonnet-latest", "Anthropic", 0.4m, new DateTime(2026, 6, 19, 12, 30, 0, 0, DateTimeKind.Utc) },
                    { new Guid("3b678d7f-7d22-4ef2-a653-8a45b0b88011"), "https://api.openai.com/v1", "", new DateTime(2026, 6, 19, 12, 30, 0, 0, DateTimeKind.Utc), false, 600, "gpt-4.1-mini", "OpenAI", 0.4m, new DateTime(2026, 6, 19, 12, 30, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a65cdf3d-b2ee-44d8-9c81-729f60a7a31c"), "http://ollama:11434", "", new DateTime(2026, 6, 19, 12, 30, 0, 0, DateTimeKind.Utc), true, 600, "llama3", "Ollama", 0.3m, new DateTime(2026, 6, 19, 12, 30, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiProviderConfigurations_Provider",
                table: "AiProviderConfigurations",
                column: "Provider",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemorySuggestions_UserProfileId_Status_CreatedUtc",
                table: "MemorySuggestions",
                columns: new[] { "UserProfileId", "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskSuggestions_UserProfileId_Status_CreatedUtc",
                table: "TaskSuggestions",
                columns: new[] { "UserProfileId", "Status", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiProviderConfigurations");

            migrationBuilder.DropTable(
                name: "MemorySuggestions");

            migrationBuilder.DropTable(
                name: "TaskSuggestions");

            migrationBuilder.DropColumn(
                name: "CompletionTokens",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "LatencyMs",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "PromptTokens",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "AgentRuns");
        }
    }
}
