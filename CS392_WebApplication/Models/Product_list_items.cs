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
    }
}
