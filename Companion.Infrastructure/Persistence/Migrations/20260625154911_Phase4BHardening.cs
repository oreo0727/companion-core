using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4BHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimeoutSeconds",
                table: "AiProviderConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<bool>(
                name: "FallbackUsed",
                table: "AgentRuns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TotalTokens",
                table: "AgentRuns",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AiProviderConfigurations",
                keyColumn: "Id",
                keyValue: new Guid("2d9e33d7-4386-4d20-8d2d-68ccdb554a7d"),
                column: "TimeoutSeconds",
                value: 30);

            migrationBuilder.UpdateData(
                table: "AiProviderConfigurations",
                keyColumn: "Id",
                keyValue: new Guid("3b678d7f-7d22-4ef2-a653-8a45b0b88011"),
                column: "TimeoutSeconds",
                value: 30);

            migrationBuilder.UpdateData(
                table: "AiProviderConfigurations",
                keyColumn: "Id",
                keyValue: new Guid("a65cdf3d-b2ee-44d8-9c81-729f60a7a31c"),
                column: "TimeoutSeconds",
                value: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeoutSeconds",
                table: "AiProviderConfigurations");

            migrationBuilder.DropColumn(
                name: "FallbackUsed",
                table: "AgentRuns");

            migrationBuilder.DropColumn(
                name: "TotalTokens",
                table: "AgentRuns");
        }
    }
}
