using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CS392_WebApplication.Models
{
    public class Product_list
    {
        [Key]
        [Column("listID")]
        public int listID { get; set; }
        [Column("userID")]
        public int userID { get; set; }
        [Column("total_price")]
        public double total_price { get; set; }
        [Column("list_type")]
        public ListType list_type { get; set; }  // enum
        [Column("created_at")]
        public DateTime created_at { get; set; }
    }

    public enum ListType
    {
        User = 0,
        School = 1
    }

}
