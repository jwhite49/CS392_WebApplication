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

        // Optional: grade level or class
        [Column("grade_level")]
        public string? grade_level { get; set; }
    }

    public enum ListType
    {
        User = 0,
        School = 1
    }
}
