using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ECommerce.Repository.Data;

#nullable disable

namespace ECommerce.Repository.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260930000100_AddProductContentAndSecondaryCategories")]
public partial class AddProductContentAndSecondaryCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "BrandLogoUrl",
            table: "Products",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "CardHighlightsJson",
            table: "Products",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "AdditionalCategoryIdsJson",
            table: "Products",
            type: "text",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "TechnicalDetails",
            table: "Products",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "DeliveryInstallationDetails",
            table: "Products",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "DocumentsDetails",
            table: "Products",
            type: "text",
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BrandLogoUrl", table: "Products");
        migrationBuilder.DropColumn(name: "CardHighlightsJson", table: "Products");
        migrationBuilder.DropColumn(name: "AdditionalCategoryIdsJson", table: "Products");
        migrationBuilder.DropColumn(name: "TechnicalDetails", table: "Products");
        migrationBuilder.DropColumn(name: "DeliveryInstallationDetails", table: "Products");
        migrationBuilder.DropColumn(name: "DocumentsDetails", table: "Products");
    }
}
