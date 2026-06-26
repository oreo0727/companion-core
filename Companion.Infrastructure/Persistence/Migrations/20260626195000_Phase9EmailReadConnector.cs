using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase9EmailReadConnector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailMessageSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FromName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    FromAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ToAddresses = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Preview = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Body = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: true),
                    ReceivedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    HasAttachments = table.Column<bool>(type: "boolean", nullable: false),
                    IsAnswered = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMessageSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailMessageSnapshots_ConnectorConnections_ConnectorConnect~",
                        column: x => x.ConnectorConnectionId,
                        principalTable: "ConnectorConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailMessageSnapshots_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ConnectorDefinitions",
                columns: new[] { "Id", "Category", "CreatedUtc", "Description", "Enabled", "Name", "Provider", "RiskLevel", "SupportsOAuth" },
                values: new object[] { new Guid("745e49ef-a388-4544-a416-8299d0fdadc0"), "Email", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Read-only local email connector that imports messages from a JSON payload.", true, "Local Email", "LocalEmail", "Low", false });

            migrationBuilder.InsertData(
                table: "ToolDefinitions",
                columns: new[] { "Id", "Category", "CreatedUtc", "Description", "Enabled", "Name", "RequiresApproval", "RiskLevel" },
                values: new object[] { new Guid("8b91435f-f204-44de-a541-997b07c654d6"), "Email", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Search read-only email snapshots for the authenticated user.", true, "EmailSearch", false, "Low" });

            migrationBuilder.InsertData(
                table: "ToolPermissions",
                columns: new[] { "Id", "Allowed", "CreatedUtc", "ToolDefinitionId", "UserProfileId" },
                values: new object[] { new Guid("f0dce30f-e80a-4c77-950c-9ef69eab0754"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("8b91435f-f204-44de-a541-997b07c654d6"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") });

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessageSnapshots_ConnectorConnectionId_ExternalId",
                table: "EmailMessageSnapshots",
                columns: new[] { "ConnectorConnectionId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessageSnapshots_UserProfileId_ReceivedUtc",
                table: "EmailMessageSnapshots",
                columns: new[] { "UserProfileId", "ReceivedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailMessageSnapshots");

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("745e49ef-a388-4544-a416-8299d0fdadc0"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("f0dce30f-e80a-4c77-950c-9ef69eab0754"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("8b91435f-f204-44de-a541-997b07c654d6"));
        }
    }
}
