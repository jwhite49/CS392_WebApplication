using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CS392_WebApplication.Models
{
    [Table("SystemLog")]
    public class SystemLog
    {
        [Key]
        [Column("log_id")]
        public int LogId { get; set; }

        [Column("timestamp")]
        public DateTime Timestamp { get; set; }

        [Column("level")]
        [Required]
        public string Level { get; set; } = string.Empty;

        [Column("event_type")]
        [Required]
        public string EventType { get; set; } = string.Empty;

        [Column("message")]
        [Required]
        public string Message { get; set; } = string.Empty;

        [Column("stack_trace")]
        public string? StackTrace { get; set; }

        [Column("user_id")]
        public string? UserId { get; set; }

        [Column("target_user_id")]
        public string? TargetUserId { get; set; }

        [Column("page")]
        public string? Page { get; set; }

        [Column("additional_data")]
        public string? AdditionalData { get; set; }
    }
}
