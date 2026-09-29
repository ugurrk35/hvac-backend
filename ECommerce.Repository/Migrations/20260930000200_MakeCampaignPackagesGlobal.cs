using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ECommerce.Repository.Data;

#nullable disable

namespace ECommerce.Repository.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260930000200_MakeCampaignPackagesGlobal")]
public partial class MakeCampaignPackagesGlobal : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_ProductCampaignPackages_Products_ProductId", table: "ProductCampaignPackages");
        migrationBuilder.DropIndex(name: "IX_ProductCampaignPackages_ProductId_IsDeleted", table: "ProductCampaignPackages");
        migrationBuilder.DropColumn(name: "ProductId", table: "ProductCampaignPackages");
        migrationBuilder.CreateIndex(name: "IX_ProductCampaignPackages_IsDeleted_IsActive_SortOrder", table: "ProductCampaignPackages", columns: new[] { "IsDeleted", "IsActive", "SortOrder" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_ProductCampaignPackages_IsDeleted_IsActive_SortOrder", table: "ProductCampaignPackages");
        migrationBuilder.AddColumn<int>(name: "ProductId", table: "ProductCampaignPackages", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.CreateIndex(name: "IX_ProductCampaignPackages_ProductId_IsDeleted", table: "ProductCampaignPackages", columns: new[] { "ProductId", "IsDeleted" });
    }
}
