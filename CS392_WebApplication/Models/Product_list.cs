using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS392_WebApplication.Models
{
    public class Product_list
    {
        [Key]
        [Column("listID")]
        public int listID { get; set; }

        [Column("userID")]
        public int userID { get; set; }   // Owner (teacher or user)

        // -----------------------------
        // Publishing System
        // -----------------------------

        [Column("publish_mode")]
        public PublishMode publish_mode { get; set; } = PublishMode.None;

        // Only used when publish_mode == Private
        [Column("private_code")]
        [MaxLength(12)]
        public string? private_code { get; set; }

        [Column("is_published")]
        public bool is_published { get; set; } = false;

        [Column("title")]
        [MaxLength(150)]
        public string? title { get; set; }

        [Column("description")]
        [MaxLength(500)]
        public string? description { get; set; }

        [Column("total_price")]
        public double total_price { get; set; }

        [Column("list_type")]
        public ListType list_type { get; set; }

        [Column("created_at")]
        public DateTime created_at { get; set; }

        [Column("updated_at")]
        public DateTime updated_at { get; set; } = DateTime.UtcNow;

        [Column("grade_level")]
        [MaxLength(50)]
        public string? grade_level { get; set; }

        // -----------------------------
        // Budget & Recommendation Features (Student Lists Only)
        // -----------------------------

        [Column("budget_amount")]
        public double? budget_amount { get; set; }

        [Column("list_category")]
        [MaxLength(100)]
        public string? list_category { get; set; }

        // Helper property to check if list is over budget
        [NotMapped]
        public bool IsOverBudget => budget_amount.HasValue && total_price > budget_amount.Value;

        // Helper property to get budget remaining
        [NotMapped]
        public double? BudgetRemaining => budget_amount.HasValue ? budget_amount.Value - total_price : null;

        // Helper property to get budget utilization percentage
        [NotMapped]
        public double? BudgetUtilizationPercent => budget_amount.HasValue && budget_amount.Value > 0 
            ? (total_price / budget_amount.Value) * 100 
            : null;
    }

    public enum ListType
    {
        User = 0,
        School = 1
    }

    public enum PublishMode
    {
        None = 0,      // Not published
        Public = 1,    // Visible to all students
        Private = 2    // Only accessible via code
    }
}
