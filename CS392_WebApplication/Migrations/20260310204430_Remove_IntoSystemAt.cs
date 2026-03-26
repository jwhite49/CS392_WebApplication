using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CS392_WebApplication.Migrations
{
    /// <inheritdoc />
    public partial class Remove_IntoSystemAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IntoSystemAt",
                table: "Products",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntoSystemAt",
                table: "Products");
        }
    }
}
