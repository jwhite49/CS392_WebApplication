using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS392_WebApplication.Models
{
    public class PublishedList_Student
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        // The published teacher list (FK → Product_list.listID)
        [Column("published_listID")]
        public int published_listID { get; set; }

        // The student who imported the list (FK → User.UserID)
        [Column("student_userID")]
        public int student_userID { get; set; }

        // The student's personal copy of the list (FK → Product_list.listID)
        [Column("student_listID")]
        public int student_listID { get; set; }

        // When the student added the list
        [Column("added_at")]
        public DateTime added_at { get; set; } = DateTime.UtcNow;

        // Whether the student marked their list as completed
        [Column("is_completed")]
        public bool is_completed { get; set; } = false;
    }
}
