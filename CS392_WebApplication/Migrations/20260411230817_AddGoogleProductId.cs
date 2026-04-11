using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CS392_WebApplication.Migrations.ProductsDb
{
    /// <inheritdoc />
    public partial class AddGoogleProductId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "google_product_id",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "google_product_id",
                table: "Products");
        }
    }
}
