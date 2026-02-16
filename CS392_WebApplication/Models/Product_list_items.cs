namespace CS392_Webusing System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS392_WebApplication.Models
{
    public class Product_list_items
    {
        [Key] public int list_items_ID { get; set; }
        [ForeignKey("Product_list")] public int list_ID { get; set; }
        [ForeignKey("Products")] public int product_ID { get; set; }
        public int quantity { get; set; }
        [Required] public float price_at_purchase { get; set; }

        public enum PurchaseType { Retail, Bulk }
        public PurchaseType purchase_type { get; set; }
    }
}
