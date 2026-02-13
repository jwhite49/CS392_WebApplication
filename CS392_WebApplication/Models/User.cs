using System.ComponentModel.DataAnnotations;

namespace CS392_WebApplication.Models
{
    public class User
    {
        [Key] public int user_id { get; set; }

        public string username { get; set; }
        public string fName { get; set; }
        public string lName { get; set; }
        public string password_hash {get; set;}

    }
}
