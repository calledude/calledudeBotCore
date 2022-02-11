using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace calledudeBot.Database.UserSession;

[Table("UserSession")]
public class UserSessionEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }

    [Required]
    public string? Username { get; set; }
    public TimeSpan WatchTime { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
