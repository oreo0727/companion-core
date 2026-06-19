using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChiefOfStaffPlanningEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Goals_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GoalSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalSuggestions_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenLoops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenLoops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenLoops_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MentionCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectSuggestions_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Goals",
                columns: new[] { "Id", "CreatedUtc", "Description", "Priority", "Status", "TargetDateUtc", "Title", "UpdatedUtc", "UserProfileId" },
                values: new object[] { new Guid("fa3f78ea-b16f-4b83-8d55-3371aa2c0d7e"), new DateTime(2026, 6, 19, 12, 20, 0, 0, DateTimeKind.Utc), "Expand Companion Core from memory and task tracking into deterministic planning support.", "High", "Active", new DateTime(2026, 6, 27, 17, 0, 0, 0, DateTimeKind.Utc), "Ship the Chief Of Staff planning layer", new DateTime(2026, 6, 19, 12, 20, 0, 0, DateTimeKind.Utc), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") });

            migrationBuilder.InsertData(
                table: "OpenLoops",
                columns: new[] { "Id", "ClosedUtc", "CreatedUtc", "Description", "Status", "Title", "UserProfileId" },
                values: new object[] { new Guid("5cddf563-fb88-4ce0-b64d-558867bd8b44"), null, new DateTime(2026, 6, 19, 12, 25, 0, 0, DateTimeKind.Utc), "Waiting on architecture sign-off before enabling broader action execution flows.", "Waiting", "Architecture sign-off for outbound approvals", new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "CreatedUtc", "Description", "Priority", "Status", "Title", "UpdatedUtc", "UserProfileId" },
                values: new object[] { new Guid("7efc59c5-27ec-48b8-9c67-f08a8da71d99"), new DateTime(2026, 5, 1, 14, 0, 0, 0, DateTimeKind.Utc), "Resume progress on the church app after the earlier planning pass stalled.", "Normal", "Active", "Church App", new DateTime(2026, 5, 1, 14, 0, 0, 0, DateTimeKind.Utc), new Guid("2d76812e-4f4f-4313-a368-5dcb29b4bf3f") });

            migrationBuilder.UpdateData(
                table: "TaskItems",
                keyColumn: "Id",
                keyValue: new Guid("0ec7d330-08fb-44fe-9b34-e0075ef75f8a"),
                column: "Description",
                value: "Queue a pending Companion Core agent run and confirm the worker moves it to completed.");

            migrationBuilder.UpdateData(
                table: "TaskItems",
                keyColumn: "Id",
                keyValue: new Guid("9995cd58-1841-4732-8c87-34a6334f6652"),
                column: "Description",
                value: "Create a sample Companion Core approval request and verify it can be approved or rejected.");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_UserProfileId_Status_TargetDateUtc",
                table: "Goals",
                columns: new[] { "UserProfileId", "Status", "TargetDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GoalSuggestions_UserProfileId_Status_CreatedUtc",
                table: "GoalSuggestions",
                columns: new[] { "UserProfileId", "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenLoops_UserProfileId_Status_CreatedUtc",
                table: "OpenLoops",
                columns: new[] { "UserProfileId", "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_UserProfileId_Status_UpdatedUtc",
                table: "Projects",
                columns: new[] { "UserProfileId", "Status", "UpdatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSuggestions_UserProfileId_Status_CreatedUtc",
                table: "ProjectSuggestions",
                columns: new[] { "UserProfileId", "Status", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Goals");

            migrationBuilder.DropTable(
                name: "GoalSuggestions");

            migrationBuilder.DropTable(
                name: "OpenLoops");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "ProjectSuggestions");

            migrationBuilder.UpdateData(
                table: "TaskItems",
                keyColumn: "Id",
                keyValue: new Guid("0ec7d330-08fb-44fe-9b34-e0075ef75f8a"),
                column: "Description",
                value: "Queue a pending agent run and confirm the worker moves it to completed.");

            migrationBuilder.UpdateData(
                table: "TaskItems",
                keyColumn: "Id",
                keyValue: new Guid("9995cd58-1841-4732-8c87-34a6334f6652"),
                column: "Description",
                value: "Create a sample approval request and verify it can be approved or rejected.");
        }
    }
}
