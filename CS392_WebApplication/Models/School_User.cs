using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS392_WebApplication.Models
{
    public class School_User
    {
        [Key, ForeignKey("User")] public int user_ID { get; set; }
        public string school_name { get; set; }
        public bool is_approved { get; set; }
    }
}
