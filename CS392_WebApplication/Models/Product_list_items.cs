using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS392_WebApplication.Models
{
    public class Product_list_items
    {
        [Key]
        [Column("list_itemsID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int list_items_ID { get; set; }

        [Column("listID")]
        public int list_ID { get; set; }

        [Column("productID")]
        public int product_ID { get; set; }

        [Column("quantity")]
        public int quantity { get; set; }

        [Required]
        [Column("price_at_purchase")]
        public float price_at_purchase { get; set; }

        public enum PurchaseType { Retail, Bulk }

        [Column("purchase_type")]
        public PurchaseType purchase_type { get; set; }

        // NEW — teacher-defined metadata for published lists
        [Column("is_required")]
        public bool is_required { get; set; } = true;

        [Column("teacher_note")]
        [MaxLength(300)]
        public string? teacher_note { get; set; }

        // NEW — recommended quantity for students
        [Column("recommended_quantity")]
        public int? recommended_quantity { get; set; }
    }
}
