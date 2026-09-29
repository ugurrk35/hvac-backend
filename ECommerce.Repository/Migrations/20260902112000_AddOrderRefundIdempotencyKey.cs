using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Repository.Migrations;

[Migration("20260902112000_AddOrderRefundIdempotencyKey")]
public partial class AddOrderRefundIdempotencyKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "IdempotencyKey", table: "OrderRefunds", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_OrderRefunds_IdempotencyKey", table: "OrderRefunds", column: "IdempotencyKey", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_OrderRefunds_IdempotencyKey", table: "OrderRefunds");
        migrationBuilder.DropColumn(name: "IdempotencyKey", table: "OrderRefunds");
    }
}
