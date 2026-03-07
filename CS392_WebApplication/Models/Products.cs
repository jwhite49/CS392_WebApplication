using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS392_WebApplication.Models
{
    public class Products
    {
        [Key]
        [Column("productID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int product_ID { get; set; }

        [Required]
        [Column("product_name")]
        public string product_name { get; set; }

        [Required]
        [Column("description")]

        public string description { get; set; }

        [Required]
        [Column("retail_price")]
        public double retail_price { get; set; }

        [Required]
        [Column("retail_URL")]

        public string retail_URL { get; set; }

        [Required]
        [Column("bulk_availability")]

        public bool bulk_availability { get; set; }

        [Column("ImageURL")]
        public string? ImageURL { get; set; }
        public DateTime IntoSystemAt { get; set; }

    }
}