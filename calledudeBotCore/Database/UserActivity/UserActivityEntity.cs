using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace calledudeBot.Database.UserActivity
{
    [Table("UserActivities")]
    public class UserActivityEntity
    {
        [Required]
        [Key]
        public string? Username { get; set; }

        public int TimesSeen { get; set; }

        [Required]
        public DateTime LastJoinDate { get; set; }
    }
}