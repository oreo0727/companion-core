using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase16DesktopAutomation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ToolDefinitions",
                columns: new[] { "Id", "Category", "CreatedUtc", "Description", "Enabled", "Name", "RequiresApproval", "RiskLevel" },
                values: new object[,]
                {
                    { new Guid("0086f4b6-8f10-436c-a822-ed38fc987e2f"), "Desktop", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Read a file from the configured desktop automation root after approval.", true, "DesktopReadFile", true, "Medium" },
                    { new Guid("35b5ef75-d468-4dd1-bbd1-4460b440813d"), "Desktop", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Set clipboard text after approval.", true, "DesktopSetClipboard", true, "High" },
                    { new Guid("3d7ff051-b995-45e6-8553-c594af5e3cee"), "Desktop", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Capture a screenshot when host screenshot tooling is available.", true, "DesktopCaptureScreenshot", false, "Low" },
                    { new Guid("66a6ba23-ba4b-460d-a702-471f8a45c11c"), "Desktop", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Move or click the mouse after approval and host configuration.", true, "DesktopMoveMouse", true, "High" },
                    { new Guid("75d850c8-c6f7-4bb0-bfe5-468f9f5ea0f8"), "Desktop", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Write a file inside the configured desktop automation root after approval.", true, "DesktopWriteFile", true, "High" },
                    { new Guid("7ab17af0-8d4e-4718-bec4-985afc4a4dcc"), "Desktop", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Read current clipboard text after approval.", true, "DesktopGetClipboard", true, "Medium" },
                    { new Guid("93c0a78b-4afe-4ad3-bccb-ee725ddf1076"), "Desktop", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Launch a local desktop application after approval.", true, "DesktopLaunchApplication", true, "High" },
                    { new Guid("ba153e06-3886-4c99-b590-2b224c470029"), "Desktop", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Send keyboard input after approval and host configuration.", true, "DesktopSendKeyboard", true, "High" },
                    { new Guid("c4da3dc9-9e2a-4f46-819e-25f41decd3d6"), "Desktop", new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), "Run a terminal command after approval and host configuration.", true, "DesktopRunTerminal", true, "High" }
                });

            migrationBuilder.InsertData(
                table: "ToolPermissions",
                columns: new[] { "Id", "Allowed", "CreatedUtc", "ToolDefinitionId", "UserProfileId" },
                values: new object[,]
                {
                    { new Guid("0d4653e4-34f1-4751-94b1-541fc69d2794"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("3d7ff051-b995-45e6-8553-c594af5e3cee"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("2b301feb-a801-48d9-84a5-726d1e21742e"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("c4da3dc9-9e2a-4f46-819e-25f41decd3d6"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("647f4e14-d051-464e-9256-d39008ad702c"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("ba153e06-3886-4c99-b590-2b224c470029"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("77ff1312-5cee-4fcf-a518-07e249ff725b"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("0086f4b6-8f10-436c-a822-ed38fc987e2f"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("7c321bae-cebe-4885-a65d-7dc4b12a7084"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("66a6ba23-ba4b-460d-a702-471f8a45c11c"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("8acda4f9-9eae-44c4-9630-aff2ff778807"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("93c0a78b-4afe-4ad3-bccb-ee725ddf1076"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("8f1c72ae-fd11-43f0-b453-eb1379063334"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("7ab17af0-8d4e-4718-bec4-985afc4a4dcc"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("9a431edf-1413-4501-821a-2a98b3c18085"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("75d850c8-c6f7-4bb0-bfe5-468f9f5ea0f8"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") },
                    { new Guid("e95d5f67-3033-4678-be92-d84850d058c0"), true, new DateTime(2026, 6, 25, 20, 0, 0, 0, DateTimeKind.Utc), new Guid("35b5ef75-d468-4dd1-bbd1-4460b440813d"), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("0d4653e4-34f1-4751-94b1-541fc69d2794"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("2b301feb-a801-48d9-84a5-726d1e21742e"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("647f4e14-d051-464e-9256-d39008ad702c"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("77ff1312-5cee-4fcf-a518-07e249ff725b"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("7c321bae-cebe-4885-a65d-7dc4b12a7084"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("8acda4f9-9eae-44c4-9630-aff2ff778807"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("8f1c72ae-fd11-43f0-b453-eb1379063334"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("9a431edf-1413-4501-821a-2a98b3c18085"));

            migrationBuilder.DeleteData(
                table: "ToolPermissions",
                keyColumn: "Id",
                keyValue: new Guid("e95d5f67-3033-4678-be92-d84850d058c0"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("0086f4b6-8f10-436c-a822-ed38fc987e2f"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("35b5ef75-d468-4dd1-bbd1-4460b440813d"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("3d7ff051-b995-45e6-8553-c594af5e3cee"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("66a6ba23-ba4b-460d-a702-471f8a45c11c"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("75d850c8-c6f7-4bb0-bfe5-468f9f5ea0f8"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("7ab17af0-8d4e-4718-bec4-985afc4a4dcc"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("93c0a78b-4afe-4ad3-bccb-ee725ddf1076"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("ba153e06-3886-4c99-b590-2b224c470029"));

            migrationBuilder.DeleteData(
                table: "ToolDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("c4da3dc9-9e2a-4f46-819e-25f41decd3d6"));
        }
    }
}
