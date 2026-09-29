using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace ECommerce.Repository.Migrations;
[Migration("20260830123000_AddReturnLabelFields")]
public partial class AddReturnLabelFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) { migrationBuilder.AddColumn<string>(name:"ReturnLabelUrl",table:"OrderReturnRequests",type:"text",nullable:true); migrationBuilder.AddColumn<string>(name:"ReturnTrackingNumber",table:"OrderReturnRequests",type:"text",nullable:true); }
    protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.DropColumn(name:"ReturnLabelUrl",table:"OrderReturnRequests"); migrationBuilder.DropColumn(name:"ReturnTrackingNumber",table:"OrderReturnRequests"); }
}
