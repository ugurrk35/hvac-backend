using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Repository.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260922223000_AddCategoryToProductDetailSettings")]
public partial class AddCategoryToProductDetailSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "CategoryId", table: "ProductDetailSettings", type: "integer", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_ProductDetailSettings_CategoryId", table: "ProductDetailSettings", column: "CategoryId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_ProductDetailSettings_CategoryId", table: "ProductDetailSettings");
        migrationBuilder.DropColumn(name: "CategoryId", table: "ProductDetailSettings");
    }
}
