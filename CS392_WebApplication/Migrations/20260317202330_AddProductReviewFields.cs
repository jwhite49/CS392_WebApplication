using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CS392_WebApplication.Migrations
{
    /// <inheritdoc />
    public partial class AddProductReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // All columns (sample_review, source_logo, source_name, item_rating, item_reviews)
            // were added manually to the database. This migration is a no-op.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: columns were added manually.
        }
    }
}
