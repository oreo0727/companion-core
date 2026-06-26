using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Companion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase12OAuthFramework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OAuthAuthorizationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConnectorProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RedirectUri = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Scopes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CodeVerifierEncrypted = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthAuthorizationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OAuthAuthorizationRequests_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OAuthConsentGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorConnectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Scopes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ConsentUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthConsentGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OAuthConsentGrants_ConnectorConnections_ConnectorConnection~",
                        column: x => x.ConnectorConnectionId,
                        principalTable: "ConnectorConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OAuthConsentGrants_ConnectorDefinitions_ConnectorDefinition~",
                        column: x => x.ConnectorDefinitionId,
                        principalTable: "ConnectorDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OAuthConsentGrants_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OAuthProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AuthorizationEndpoint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TokenEndpoint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RevocationEndpoint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DefaultScopes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ClientIdSecretName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ClientSecretSecretName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthProviderConfigurations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ConnectorDefinitions",
                columns: new[] { "Id", "Category", "CreatedUtc", "Description", "Enabled", "Name", "Provider", "RiskLevel", "SupportsOAuth" },
                values: new object[,]
                {
                    { new Guid("179c4f6c-476a-457a-8c51-9466aa9c2d73"), "Calendar", new DateTime(2026, 6, 26, 21, 0, 0, 0, DateTimeKind.Utc), "Read-only Microsoft Calendar connector prepared for OAuth-based event synchronization.", true, "Microsoft Calendar", "MicrosoftCalendar", "Low", true },
                    { new Guid("239e0f35-92c9-4ad8-a835-94238efee721"), "Files", new DateTime(2026, 6, 26, 21, 0, 0, 0, DateTimeKind.Utc), "Read-only Google Drive connector prepared for OAuth-based document synchronization.", true, "Google Drive", "GoogleDrive", "Low", true },
                    { new Guid("3799b2f4-9691-4d92-a5ff-b36b8bc7c47d"), "Email", new DateTime(2026, 6, 26, 21, 0, 0, 0, DateTimeKind.Utc), "Read-only Gmail connector prepared for OAuth-based message synchronization.", true, "Gmail", "Gmail", "Low", true },
                    { new Guid("9fbe2c8b-64d0-4f33-99dd-d5ed38e3f112"), "Calendar", new DateTime(2026, 6, 26, 21, 0, 0, 0, DateTimeKind.Utc), "Read-only Google Calendar connector prepared for OAuth-based event synchronization.", true, "Google Calendar", "GoogleCalendar", "Low", true },
                    { new Guid("bc0465b0-d97b-4cc6-b363-f10104766a24"), "Files", new DateTime(2026, 6, 26, 21, 0, 0, 0, DateTimeKind.Utc), "Read-only OneDrive connector prepared for OAuth-based document synchronization.", true, "OneDrive", "OneDrive", "Low", true },
                    { new Guid("e2942c91-6a65-47b0-8978-a914c5b8bcf4"), "Email", new DateTime(2026, 6, 26, 21, 0, 0, 0, DateTimeKind.Utc), "Read-only Outlook Mail connector prepared for OAuth-based message synchronization.", true, "Outlook Mail", "OutlookMail", "Low", true }
                });

            migrationBuilder.InsertData(
                table: "OAuthProviderConfigurations",
                columns: new[] { "Id", "AuthorizationEndpoint", "ClientIdSecretName", "ClientSecretSecretName", "CreatedUtc", "DefaultScopes", "DisplayName", "Enabled", "Provider", "RevocationEndpoint", "TokenEndpoint", "UpdatedUtc" },
                values: new object[,]
                {
                    { new Guid("b5e7a2aa-9845-4f95-b02f-5213d4289cb8"), "https://login.microsoftonline.com/common/oauth2/v2.0/authorize", "OAuth:Microsoft:ClientId", "OAuth:Microsoft:ClientSecret", new DateTime(2026, 6, 26, 21, 0, 0, 0, DateTimeKind.Utc), "openid email profile offline_access", "Microsoft", true, "Microsoft", null, "https://login.microsoftonline.com/common/oauth2/v2.0/token", new DateTime(2026, 6, 26, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("eec80fab-e287-435d-b6f1-a98d1967a115"), "https://accounts.google.com/o/oauth2/v2/auth", "OAuth:Google:ClientId", "OAuth:Google:ClientSecret", new DateTime(2026, 6, 26, 21, 0, 0, 0, DateTimeKind.Utc), "openid email profile", "Google", true, "Google", "https://oauth2.googleapis.com/revoke", "https://oauth2.googleapis.com/token", new DateTime(2026, 6, 26, 21, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthAuthorizationRequests_State",
                table: "OAuthAuthorizationRequests",
                column: "State",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthAuthorizationRequests_UserProfileId_Provider_CreatedUtc",
                table: "OAuthAuthorizationRequests",
                columns: new[] { "UserProfileId", "Provider", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthConsentGrants_ConnectorConnectionId",
                table: "OAuthConsentGrants",
                column: "ConnectorConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthConsentGrants_ConnectorDefinitionId",
                table: "OAuthConsentGrants",
                column: "ConnectorDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthConsentGrants_UserProfileId_ConnectorConnectionId",
                table: "OAuthConsentGrants",
                columns: new[] { "UserProfileId", "ConnectorConnectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthConsentGrants_UserProfileId_Provider_Subject",
                table: "OAuthConsentGrants",
                columns: new[] { "UserProfileId", "Provider", "Subject" });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthProviderConfigurations_Provider",
                table: "OAuthProviderConfigurations",
                column: "Provider",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OAuthAuthorizationRequests");

            migrationBuilder.DropTable(
                name: "OAuthConsentGrants");

            migrationBuilder.DropTable(
                name: "OAuthProviderConfigurations");

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("179c4f6c-476a-457a-8c51-9466aa9c2d73"));

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("239e0f35-92c9-4ad8-a835-94238efee721"));

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("3799b2f4-9691-4d92-a5ff-b36b8bc7c47d"));

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("9fbe2c8b-64d0-4f33-99dd-d5ed38e3f112"));

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("bc0465b0-d97b-4cc6-b363-f10104766a24"));

            migrationBuilder.DeleteData(
                table: "ConnectorDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("e2942c91-6a65-47b0-8978-a914c5b8bcf4"));
        }
    }
}
