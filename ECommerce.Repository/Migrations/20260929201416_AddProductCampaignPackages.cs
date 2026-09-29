using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ECommerce.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCampaignPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CampaignSnapshotJson",
                table: "CartItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductCampaignPackageId",
                table: "CartItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPriceSnapshot",
                table: "CartItems",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductCampaignPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    StartingPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCampaignPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCampaignPackages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductCampaignLocationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductCampaignPackageId = table.Column<int>(type: "integer", nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PriceAdjustment = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCampaignLocationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCampaignLocationRules_ProductCampaignPackages_Produc~",
                        column: x => x.ProductCampaignPackageId,
                        principalTable: "ProductCampaignPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductCampaignLookupGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductCampaignPackageId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCampaignLookupGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCampaignLookupGroups_ProductCampaignPackages_Product~",
                        column: x => x.ProductCampaignPackageId,
                        principalTable: "ProductCampaignPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductCampaignLookupOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductCampaignLookupGroupId = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    PriceAdjustment = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCampaignLookupOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCampaignLookupOptions_ProductCampaignLookupGroups_Pr~",
                        column: x => x.ProductCampaignLookupGroupId,
                        principalTable: "ProductCampaignLookupGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCampaignLocationRules_ProductCampaignPackageId_City_~",
                table: "ProductCampaignLocationRules",
                columns: new[] { "ProductCampaignPackageId", "City", "District" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCampaignLookupGroups_ProductCampaignPackageId_Code",
                table: "ProductCampaignLookupGroups",
                columns: new[] { "ProductCampaignPackageId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCampaignLookupOptions_ProductCampaignLookupGroupId",
                table: "ProductCampaignLookupOptions",
                column: "ProductCampaignLookupGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCampaignPackages_ProductId_IsDeleted",
                table: "ProductCampaignPackages",
                columns: new[] { "ProductId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductCampaignLocationRules");

            migrationBuilder.DropTable(
                name: "ProductCampaignLookupOptions");

            migrationBuilder.DropTable(
                name: "ProductCampaignLookupGroups");

            migrationBuilder.DropTable(
                name: "ProductCampaignPackages");

            migrationBuilder.DropColumn(
                name: "CampaignSnapshotJson",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ProductCampaignPackageId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "UnitPriceSnapshot",
                table: "CartItems");
        }
    }
}
