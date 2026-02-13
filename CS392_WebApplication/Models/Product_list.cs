using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS392_WebApplication.Models
{
    public class Product_list
    {
        [Key]public int list_ID { get; set; }
        [ForeignKey("User")] public int user_ID { get; set; }
        [Required] public float total_price { get; set; }
        [Required] public string list_type { get; set; }
        //whether list type is for user / school 
        //could possibly chage it to bool, false for schoool , true for user 
        public string amazon_wishlist_url { get; set; }
        [Required] public DateTime created_at { get; set; }
    }
}
