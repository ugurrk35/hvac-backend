using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace ECommerce.Repository.Migrations;
[Migration("20260830124000_AddProductRelatedPresentation")]
public partial class AddProductRelatedPresentation : Migration { protected override void Up(MigrationBuilder m){m.AddColumn<int>(name:"RecommendationType",table:"ProductRelateds",type:"integer",nullable:false,defaultValue:0);m.AddColumn<bool>(name:"ShowOnProductPage",table:"ProductRelateds",type:"boolean",nullable:false,defaultValue:true);m.AddColumn<bool>(name:"ShowInCart",table:"ProductRelateds",type:"boolean",nullable:false,defaultValue:true);} protected override void Down(MigrationBuilder m){m.DropColumn(name:"RecommendationType",table:"ProductRelateds");m.DropColumn(name:"ShowOnProductPage",table:"ProductRelateds");m.DropColumn(name:"ShowInCart",table:"ProductRelateds");} }
