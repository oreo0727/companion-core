using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase17HomeAutomation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeDeviceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DeviceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Room = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CapabilitiesJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    LastSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeDeviceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeDeviceSnapshots_ConnectorConnections_ConnectorConnectio~",
                        column: x => x.ConnectorConnectionId,
                        principalTable: "ConnectorConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HomeDeviceSnapshots_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HomeSensorSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SensorType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Room = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ObservedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeSensorSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomeSensorSnapshots_ConnectorConnections_ConnectorConnectio~",
                        column: x => x.ConnectorConnectionId,
                        principalTable: "ConnectorConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HomeSensorSnapshots_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ConnectorDefinitions",
                columns: new[] { "Id", "Category", "CreatedUtc", "Description", "Enabled", "Name", "Provider", "RiskLevel", "SupportsOAuth" },
                values: new object[,]
                {
                    { new Guid("226f2d26-fd3f-4028-a005-a5b08feff420"), "Home", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Read-only SmartThings connector prepared for device and sensor synchronization.", true, "SmartThings", "SmartThings", "Low", false },
                    { new Guid("2ef85991-c42f-4f55-ac45-f8a5f1e79f90"), "Home", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Read-only Shelly connector prepared for relay and sensor synchronization.", true, "Shelly", "Shelly", "Low", false },
                    { new Guid("437d6510-e691-4f42-b734-b8fef168fc07"), "Home", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Read-only Home Assistant connector prepared for device and sensor synchronization.", true, "Home Assistant", "HomeAssistant", "Low", false },
                    { new Guid("6f3be5c4-f3f6-41f1-a836-5c01c7a27b45"), "Home", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Read-only Hue connector prepared for light and sensor synchronization.", true, "Hue", "Hue", "Low", false },
                    { new Guid("7aa51df0-1058-4857-b19a-e474df9b1bc3"), "Home", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Read-only MQTT home connector prepared for device and sensor topic snapshots.", true, "MQTT", "MQTT", "Low", false },
                    { new Guid("a499deef-17fc-4500-aa70-34ed846f54a2"), "Home", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Read-only ESPHome connector prepared for device and sensor synchronization.", true, "ESPHome", "ESPHome", "Low", false },
                    { new Guid("b7bf35f0-246e-449d-aa59-1b2b4fedfab9"), "Home", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Read-only local home automation connector that imports devices and sensors from JSON.", true, "Local Home", "LocalHome", "Low", false }
                });

            migrationBuilder.InsertData(
                table: "ToolDefinitions",
                columns: new[] { "Id", "Category", "CreatedUtc", "Description", "Enabled", "Name", "RequiresApproval", "RiskLevel" },
                values: new object[,]
                {
                    { new Guid("28e62394-96e8-4a82-a704-87c231168b89"), "Home", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Execute an approved home automation action.", true, "HomeExecuteAction", true, "High" },
                    { new Guid("958fa14b-d110-4876-8910-d5ce7fda96cf"), "Home", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "List current home device and sensor snapshots.", true, "HomeStatus", false, "Low" }
                });

            migrationBuilder.InsertData(
                table: "ToolPermissions",
                columns: new[] { "Id", "Allowed", "CreatedUtc", "ToolDefinitionId", "UserProfileId" },
                values: new object[,]
                {
                    { new Guid("03a2bd81-1828-47bb-bd13-63906bf36ffc"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("958fa14b-d110-4876-8910-d5ce7fda96cf"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("1146badc-054a-4c41-8e28-c62971546bc9"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("28e62394-96e8-4a82-a704-87c231168b89"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeDeviceSnapshots_ConnectorConnectionId_ExternalId",
                table: "HomeDeviceSnapshots",
                columns: new[] { "ConnectorConnectionId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HomeDeviceSnapshots_UserProfileId_DeviceType_State",
                table: "HomeDeviceSnapshots",
                columns: new[] { "UserProfileId", "DeviceType", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_HomeSensorSnapshots_ConnectorConnectionId_ExternalId",
                table: "HomeSensorSnapshots",
                columns: new[] { "ConnectorConnectionId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HomeSensorSnapshots_UserProfileId_SensorType",
                table: "HomeSensorSnapshots",
                columns: new[] { "UserProfileId", "SensorType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeDeviceSnapshots");

            migrationBuilder.DropTable(
                name: "HomeSensorSnapshots");

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("226f2d26-fd3f-4028-a005-a5b08feff420"));

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("2ef85991-c42f-4f55-ac45-f8a5f1e79f90"));

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("437d6510-e691-4f42-b734-b8fef168fc07"));

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("6f3be5c4-f3f6-41f1-a836-5c01c7a27b45"));

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("7aa51df0-1058-4857-b19a-e474df9b1bc3"));

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("a499deef-17fc-4500-aa70-34ed846f54a2"));

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("b7bf35f0-246e-449d-aa59-1b2b4fedfab9"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("03a2bd81-1828-47bb-bd13-63906bf36ffc"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("1146badc-054a-4c41-8e28-c62971546bc9"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("28e62394-96e8-4a82-a704-87c231168b89"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("958fa14b-d110-4876-8910-d5ce7fda96cf"));
        }
    }
}
