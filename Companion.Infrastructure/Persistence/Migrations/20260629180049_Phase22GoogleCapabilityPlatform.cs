using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase22GoogleCapabilityPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Email = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Organization = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    BirthdayUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PhotoUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactSnapshots_ConnectorConnections_ConnectorConnectionId",
                        column: x => x.ConnectorConnectionId,
                        principalTable: "ConnectorConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContactSnapshots_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ConnectorDefinitions",
                columns: new[] { "Id", "Category", "CreatedUtc", "Description", "Enabled", "Name", "Provider", "RiskLevel", "SupportsOAuth" },
                values: new object[] { new Guid("99b4da63-30e0-4ac2-9115-2f1207e6cc50"), "People", new DateTime(2026, 6, 26, 21, 0, 0, 0, DateTimeKind.Utc), "Read-only Google People connector prepared for OAuth-based contact synchronization.", true, "Google People", "GooglePeople", "Low", true });

            migrationBuilder.UpdateData(
                table: "OAuthProviderConfigurations",
                keyColumn: "Id",
                keyValue: new Guid("eec80fab-e287-435d-b6f1-a98d1967a115"),
                column: "DefaultScopes",
                value: "openid email profile https://www.googleapis.com/auth/calendar.readonly https://www.googleapis.com/auth/gmail.readonly https://www.googleapis.com/auth/gmail.compose https://www.googleapis.com/auth/drive.readonly https://www.googleapis.com/auth/contacts.readonly");

            migrationBuilder.InsertData(
                table: "ToolDefinitions",
                columns: new[] { "Id", "Category", "CreatedUtc", "Description", "Enabled", "Name", "RequiresApproval", "RiskLevel" },
                values: new object[,]
                {
                    { new Guid("09e97211-e943-490a-98a4-dd9e54491e68"), "Email", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Create a draft email after approval; sending is not supported.", true, "CreateDraftEmail", true, "Medium" },
                    { new Guid("2f4e5670-78ed-45c7-91ac-5e594b2350ad"), "Email", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Search read-only email snapshots through the email capability.", true, "SearchEmail", false, "Low" },
                    { new Guid("3d245f82-83fc-4463-9de4-ad5755b0ed3d"), "Calendar", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Find open calendar focus blocks and conflicts through the calendar capability.", true, "FindFreeTime", false, "Low" },
                    { new Guid("8afc4bb8-39d7-44bb-b819-bd0809616ccd"), "Files", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Read file metadata and preview text through the file capability.", true, "ReadDocument", false, "Low" },
                    { new Guid("c2a652a6-8993-44a2-8544-199d72f1c82f"), "Calendar", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Retrieve upcoming meetings through the calendar capability.", true, "GetCalendarEvents", false, "Low" },
                    { new Guid("d5e08a36-ee5a-4ac1-aea2-45a6bbef5425"), "Email", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Read a single email snapshot through the email capability.", true, "ReadEmail", false, "Low" },
                    { new Guid("e20dbec7-6c98-492c-a77a-5f0eb708a09e"), "People", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Find contacts through the people capability.", true, "FindContact", false, "Low" },
                    { new Guid("fbf2bcda-6478-4a38-b634-a5bf61b3c6fb"), "Files", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Search file snapshots through the file capability.", true, "SearchDrive", false, "Low" }
                });

            migrationBuilder.InsertData(
                table: "ToolPermissions",
                columns: new[] { "Id", "Allowed", "CreatedUtc", "ToolDefinitionId", "UserProfileId" },
                values: new object[,]
                {
                    { new Guid("0e4b27dd-b922-4f48-b0d7-602da712bc69"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("c2a652a6-8993-44a2-8544-199d72f1c82f"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("252b24ec-2087-4c27-9297-848ba49c03e3"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("e20dbec7-6c98-492c-a77a-5f0eb708a09e"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("3becf8fe-0b33-471e-8e29-202a263b94dd"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("fbf2bcda-6478-4a38-b634-a5bf61b3c6fb"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("55158f57-0afa-44f9-b1b4-b125935b223e"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("09e97211-e943-490a-98a4-dd9e54491e68"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("86c0fc68-78e3-4e21-8f7b-15551e7f19ca"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("2f4e5670-78ed-45c7-91ac-5e594b2350ad"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("9485d805-b4ed-4390-889a-5288662ac1bc"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("d5e08a36-ee5a-4ac1-aea2-45a6bbef5425"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("c7ec42f6-e4db-490c-9d16-625f7d8883c1"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("8afc4bb8-39d7-44bb-b819-bd0809616ccd"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("dd13d0fb-0997-4adf-85c2-688d7e214aa9"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("3d245f82-83fc-4463-9de4-ad5755b0ed3d"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactSnapshots_ConnectorConnectionId_ExternalId",
                table: "ContactSnapshots",
                columns: new[] { "ConnectorConnectionId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactSnapshots_UserProfileId_DisplayName",
                table: "ContactSnapshots",
                columns: new[] { "UserProfileId", "DisplayName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactSnapshots");

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("99b4da63-30e0-4ac2-9115-2f1207e6cc50"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("0e4b27dd-b922-4f48-b0d7-602da712bc69"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("252b24ec-2087-4c27-9297-848ba49c03e3"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("3becf8fe-0b33-471e-8e29-202a263b94dd"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("55158f57-0afa-44f9-b1b4-b125935b223e"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("86c0fc68-78e3-4e21-8f7b-15551e7f19ca"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("9485d805-b4ed-4390-889a-5288662ac1bc"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("c7ec42f6-e4db-490c-9d16-625f7d8883c1"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("dd13d0fb-0997-4adf-85c2-688d7e214aa9"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("09e97211-e943-490a-98a4-dd9e54491e68"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("2f4e5670-78ed-45c7-91ac-5e594b2350ad"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("3d245f82-83fc-4463-9de4-ad5755b0ed3d"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("8afc4bb8-39d7-44bb-b819-bd0809616ccd"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("c2a652a6-8993-44a2-8544-199d72f1c82f"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("d5e08a36-ee5a-4ac1-aea2-45a6bbef5425"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("e20dbec7-6c98-492c-a77a-5f0eb708a09e"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("fbf2bcda-6478-4a38-b634-a5bf61b3c6fb"));

            migrationBuilder.UpdateData(
                table: "OAuthProviderConfigurations",
                keyColumn: "Id",
                keyValue: new Guid("eec80fab-e287-435d-b6f1-a98d1967a115"),
                column: "DefaultScopes",
                value: "openid email profile");
        }
    }
}
