using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Repository.Migrations;

[Migration("20260902110000_AddOrderStatusHistoryEventType")]
public partial class AddOrderStatusHistoryEventType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(name: "EventType", table: "OrderStatusHistories", type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "status");
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "EventType", table: "OrderStatusHistories");
}
