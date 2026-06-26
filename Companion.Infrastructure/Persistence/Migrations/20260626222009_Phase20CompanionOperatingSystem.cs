using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase20CompanionOperatingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperatingSystemRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoutineType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    InsightsJson = table.Column<string>(type: "text", nullable: false),
                    ActionsJson = table.Column<string>(type: "text", nullable: false),
                    ForecastJson = table.Column<string>(type: "text", nullable: false),
                    ScheduledAgentRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatingSystemRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatingSystemRuns_AgentRuns_ScheduledAgentRunId",
                        column: x => x.ScheduledAgentRunId,
                        principalTable: "AgentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OperatingSystemRuns_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperatingSystemRuns_ScheduledAgentRunId",
                table: "OperatingSystemRuns",
                column: "ScheduledAgentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatingSystemRuns_UserProfileId_RoutineType_CreatedUtc",
                table: "OperatingSystemRuns",
                columns: new[] { "UserProfileId", "RoutineType", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperatingSystemRuns");
        }
    }
}
