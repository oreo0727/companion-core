using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase10NotificationsReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreferenceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LeadTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MetadataJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DueUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reminders_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Reminders_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "NotificationPreferences",
                columns: new[] { "Id", "CreatedUtc", "InAppEnabled", "LeadTimeMinutes", "PreferenceType", "UpdatedUtc", "UserProfileId" },
                values: new object[,]
                {
                    { new Guid("1c72af38-7524-4261-93d3-f53cc9deff4e"), new DateTime(2026, 6, 19, 12, 0, 0, 0, DateTimeKind.Utc), true, 0, "ManualReminder", new DateTime(2026, 6, 19, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("877a581f-50da-40ee-87f9-789cbb8d9c54"), new DateTime(2026, 6, 19, 12, 0, 0, 0, DateTimeKind.Utc), true, 1440, "TaskDue", new DateTime(2026, 6, 19, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("a1331ba2-78d7-40ab-85fd-51d5b5e350f0"), new DateTime(2026, 6, 19, 12, 0, 0, 0, DateTimeKind.Utc), true, 0, "ApprovalPending", new DateTime(2026, 6, 19, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("dafd4e25-0a90-43c3-833e-aa4900a4da22"), new DateTime(2026, 6, 19, 12, 0, 0, 0, DateTimeKind.Utc), true, 60, "CalendarEvent", new DateTime(2026, 6, 19, 12, 0, 0, 0, DateTimeKind.Utc), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") }
                });

            migrationBuilder.InsertData(
                table: "ToolDefinitions",
                columns: new[] { "Id", "Category", "CreatedUtc", "Description", "Enabled", "Name", "RequiresApproval", "RiskLevel" },
                values: new object[,]
                {
                    { new Guid("1c82899c-debe-4f89-8c20-63c13804c81c"), "Notifications", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Create an in-app reminder for the authenticated user.", true, "CreateReminder", false, "Low" },
                    { new Guid("a442411b-d69f-4e6a-a76e-7aa4ef8cc388"), "Notifications", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "List in-app notifications for the authenticated user.", true, "ListNotifications", false, "Low" }
                });

            migrationBuilder.InsertData(
                table: "ToolPermissions",
                columns: new[] { "Id", "Allowed", "CreatedUtc", "ToolDefinitionId", "UserProfileId" },
                values: new object[,]
                {
                    { new Guid("1bfd0877-5b0c-451d-b0e4-a426b2ea8f7d"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("1c82899c-debe-4f89-8c20-63c13804c81c"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("2f34893c-23fb-429a-ac70-68007e702136"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("a442411b-d69f-4e6a-a76e-7aa4ef8cc388"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserProfileId_PreferenceType",
                table: "NotificationPreferences",
                columns: new[] { "UserProfileId", "PreferenceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserProfileId_Status_CreatedUtc",
                table: "Notifications",
                columns: new[] { "UserProfileId", "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserProfileId_Type_EntityType_EntityId",
                table: "Notifications",
                columns: new[] { "UserProfileId", "Type", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_NotificationId",
                table: "Reminders",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_UserProfileId_SourceType_SourceId",
                table: "Reminders",
                columns: new[] { "UserProfileId", "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_UserProfileId_Status_DueUtc",
                table: "Reminders",
                columns: new[] { "UserProfileId", "Status", "DueUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "Reminders");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("1bfd0877-5b0c-451d-b0e4-a426b2ea8f7d"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("2f34893c-23fb-429a-ac70-68007e702136"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("1c82899c-debe-4f89-8c20-63c13804c81c"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("a442411b-d69f-4e6a-a76e-7aa4ef8cc388"));
        }
    }
}
