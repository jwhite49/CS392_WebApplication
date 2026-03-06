using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CS392_WebApplication.Migrations
{
    /// <inheritdoc />
    public partial class ProductList_UpdateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "amazon_wishlist_url",
                table: "Product_list");

            migrationBuilder.AlterColumn<int>(
                name: "list_type",
                table: "Product_list",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "list_type",
                table: "Product_list",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "amazon_wishlist_url",
                table: "Product_list",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
