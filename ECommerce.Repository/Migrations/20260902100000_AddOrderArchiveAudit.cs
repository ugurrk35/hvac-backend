using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Repository.Migrations;

[Migration("20260902100000_AddOrderArchiveAudit")]
public partial class AddOrderArchiveAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ArchiveReason", table: "Orders", type: "character varying(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "ArchivedAt", table: "Orders", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<int>(name: "ArchivedByUserId", table: "Orders", type: "integer", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Orders_IsDeleted_ArchivedAt", table: "Orders", columns: new[] { "IsDeleted", "ArchivedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Orders_IsDeleted_ArchivedAt", table: "Orders");
        migrationBuilder.DropColumn(name: "ArchiveReason", table: "Orders");
        migrationBuilder.DropColumn(name: "ArchivedAt", table: "Orders");
        migrationBuilder.DropColumn(name: "ArchivedByUserId", table: "Orders");
    }
}
