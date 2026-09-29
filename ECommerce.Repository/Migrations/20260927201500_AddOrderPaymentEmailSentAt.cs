using ECommerce.Repository.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Repository.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260927201500_AddOrderPaymentEmailSentAt")]
public partial class AddOrderPaymentEmailSentAt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.AddColumn<DateTime>(name: "PaymentEmailSentAt", table: "Orders", type: "timestamp with time zone", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn(name: "PaymentEmailSentAt", table: "Orders");
}
