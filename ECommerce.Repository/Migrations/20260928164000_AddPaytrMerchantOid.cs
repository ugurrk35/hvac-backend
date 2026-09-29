using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Repository.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260928164000_AddPaytrMerchantOid")]
public partial class AddPaytrMerchantOid : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PaytrMerchantOid",
            table: "Orders",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Orders_PaytrMerchantOid",
            table: "Orders",
            column: "PaytrMerchantOid",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Orders_PaytrMerchantOid", table: "Orders");
        migrationBuilder.DropColumn(name: "PaytrMerchantOid", table: "Orders");
    }
}
