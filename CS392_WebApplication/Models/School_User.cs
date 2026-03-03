using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS392_WebApplication.Models
{
    public class School_User
    {
        [Key]
        [ForeignKey("User")]
        [Column("userID")]
        public int userID { get; set; }
        [Column("school_name")]
        public string school_name { get; set; }
        [Column("is_approved")]
        public bool is_approved { get; set; }
    }
}
