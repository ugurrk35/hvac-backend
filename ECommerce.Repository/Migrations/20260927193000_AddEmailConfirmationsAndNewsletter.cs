using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Repository.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260927193000_AddEmailConfirmationsAndNewsletter")]
public partial class AddEmailConfirmationsAndNewsletter : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EmailConfirmationTokens",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(type: "integer", nullable: true),
                Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                Purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailConfirmationTokens", x => x.Id);
                table.ForeignKey(name: "FK_EmailConfirmationTokens_AspNetUsers_UserId", column: x => x.UserId, principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "NewsletterSubscriptions",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UnsubscribedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UnsubscribeToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Source = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_NewsletterSubscriptions", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_EmailConfirmationTokens_Purpose_TokenHash", table: "EmailConfirmationTokens", columns: new[] { "Purpose", "TokenHash" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_EmailConfirmationTokens_Email_Purpose_UsedAt", table: "EmailConfirmationTokens", columns: new[] { "Email", "Purpose", "UsedAt" });
        migrationBuilder.CreateIndex(name: "IX_EmailConfirmationTokens_UserId", table: "EmailConfirmationTokens", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_NewsletterSubscriptions_Email", table: "NewsletterSubscriptions", column: "Email", unique: true);
        migrationBuilder.CreateIndex(name: "IX_NewsletterSubscriptions_UnsubscribeToken", table: "NewsletterSubscriptions", column: "UnsubscribeToken", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EmailConfirmationTokens");
        migrationBuilder.DropTable(name: "NewsletterSubscriptions");
    }
}
