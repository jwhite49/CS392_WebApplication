using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS392_WebApplication.Models
{
    [Table("User")]
    public class User
    {
        [Key]
        [Column("userID")]
        public int UserID { get; set; }

        [Required]
        [Column("username")]
        [MaxLength(50)]
        public string Username { get; set; } = null!;

        [Required]
        [Column("email")]
        [MaxLength(50)]
        public string Email { get; set; } = null!;

        [Required]
        [Column("password")]
        [MaxLength(50)]
        public string Password { get; set; } = null!;

        [Required]
        [Column("fname")]
        [MaxLength(20)]
        public string FirstName { get; set; } = null!;

        [Required]
        [Column("lName")]
        [MaxLength(20)]
        public string LastName { get; set; } = null!;
    }
}