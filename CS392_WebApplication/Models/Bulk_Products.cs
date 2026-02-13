using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS392_WebApplication.Models
{
    public class Bulk_Products
    {
        [Key] public int bulk_ID { get; set; }
        [ForeignKey("Products")] public int product_ID { get; set; }
        public string bulk_URL { get; set; }
        public float bulk_price { get; set; } 
        public int min_quantity { get; set; }
    }
}
