using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase14VoicePlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VoiceSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SpeechToTextProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TextToSpeechProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsWakeSession = table.Column<bool>(type: "boolean", nullable: false),
                    WakePhrase = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MetadataJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    StartedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActivityUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InterruptedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoiceSessions_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoiceSessions_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoiceInteractions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoiceSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Text = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    AudioReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LatencyMs = table.Column<long>(type: "bigint", nullable: true),
                    MetadataJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoiceInteractions_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VoiceInteractions_VoiceSessions_VoiceSessionId",
                        column: x => x.VoiceSessionId,
                        principalTable: "VoiceSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceInteractions_UserProfileId_CreatedUtc",
                table: "VoiceInteractions",
                columns: new[] { "UserProfileId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceInteractions_VoiceSessionId_CreatedUtc",
                table: "VoiceInteractions",
                columns: new[] { "VoiceSessionId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VoiceSessions_ConversationId",
                table: "VoiceSessions",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceSessions_UserProfileId_Status_LastActivityUtc",
                table: "VoiceSessions",
                columns: new[] { "UserProfileId", "Status", "LastActivityUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VoiceInteractions");

            migrationBuilder.DropTable(
                name: "VoiceSessions");
        }
    }
}
