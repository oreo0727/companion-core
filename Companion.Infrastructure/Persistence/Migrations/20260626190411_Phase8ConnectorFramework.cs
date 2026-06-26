using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase8ConnectorFramework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConnectorDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SupportsOAuth = table.Column<bool>(type: "boolean", nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConnectorConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AccessTokenEncrypted = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    RefreshTokenEncrypted = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    ExpiresUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectorConnections_ConnectorDefinitions_ConnectorDefiniti~",
                        column: x => x.ConnectorDefinitionId,
                        principalTable: "ConnectorDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConnectorConnections_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalendarEventSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAllDay = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEventSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarEventSnapshots_ConnectorConnections_ConnectorConnec~",
                        column: x => x.ConnectorConnectionId,
                        principalTable: "ConnectorConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CalendarEventSnapshots_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConnectorSyncRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ItemsSynced = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorSyncRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectorSyncRuns_ConnectorConnections_ConnectorConnectionId",
                        column: x => x.ConnectorConnectionId,
                        principalTable: "ConnectorConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConnectorSyncRuns_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ConnectorDefinitions",
                columns: new[] { "Id", "Category", "CreatedUtc", "Description", "Enabled", "Name", "Provider", "RiskLevel", "SupportsOAuth" },
                values: new object[] { new Guid("fb132d85-476e-48d2-81cb-4e6a1bf09cf5"), "Calendar", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Read-only local calendar connector that imports upcoming events from a JSON payload.", true, "Local Calendar", "LocalCalendar", "Low", false });

            migrationBuilder.InsertData(
                table: "ToolDefinitions",
                columns: new[] { "Id", "Category", "CreatedUtc", "Description", "Enabled", "Name", "RequiresApproval", "RiskLevel" },
                values: new object[] { new Guid("0ddf4583-81b6-4e2d-a3d6-738066b13d8c"), "Calendar", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Retrieve upcoming calendar events for the authenticated user.", true, "CalendarEvents", false, "Low" });

            migrationBuilder.InsertData(
                table: "ToolPermissions",
                columns: new[] { "Id", "Allowed", "CreatedUtc", "ToolDefinitionId", "UserProfileId" },
                values: new object[] { new Guid("e1fc039a-1ca6-426b-9a9a-29873fe46f76"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("0ddf4583-81b6-4e2d-a3d6-738066b13d8c"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEventSnapshots_ConnectorConnectionId_ExternalId",
                table: "CalendarEventSnapshots",
                columns: new[] { "ConnectorConnectionId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEventSnapshots_UserProfileId_StartUtc",
                table: "CalendarEventSnapshots",
                columns: new[] { "UserProfileId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorConnections_ConnectorDefinitionId",
                table: "ConnectorConnections",
                column: "ConnectorDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorConnections_UserProfileId_ConnectorDefinitionId_Di~",
                table: "ConnectorConnections",
                columns: new[] { "UserProfileId", "ConnectorDefinitionId", "DisplayName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorDefinitions_Provider",
                table: "ConnectorDefinitions",
                column: "Provider",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorSyncRuns_ConnectorConnectionId",
                table: "ConnectorSyncRuns",
                column: "ConnectorConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorSyncRuns_UserProfileId_StartedUtc",
                table: "ConnectorSyncRuns",
                columns: new[] { "UserProfileId", "StartedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarEventSnapshots");

            migrationBuilder.DropTable(
                name: "ConnectorSyncRuns");

            migrationBuilder.DropTable(
                name: "ConnectorConnections");

            migrationBuilder.DropTable(
                name: "ConnectorDefinitions");

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("e1fc039a-1ca6-426b-9a9a-29873fe46f76"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("0ddf4583-81b6-4e2d-a3d6-738066b13d8c"));
        }
    }
}
