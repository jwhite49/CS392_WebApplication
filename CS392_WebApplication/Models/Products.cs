using System.ComponentModel.DataAnnotations;

namespace CS392_WebApplication.Models
{
    public class Products
    {
        [Key] public int product_ID { get; set; }
        [Required] public string product_name { get; set; }
        [Required] public string description { get; set; }
        [Required] public float retail_price { get; set; }
        [Required] public string retail_URL { get; set; }
        [Required] public bool bulk_availability { get; set; }

    }
}
