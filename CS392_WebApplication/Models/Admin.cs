using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS392_WebApplication.Models
{
    public class Admin
    {
        [Key, ForeignKey("User")]
        public int AdminID { get; set; }

    }
}