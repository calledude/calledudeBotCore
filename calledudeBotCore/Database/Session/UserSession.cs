using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace calledudeBot.Database.Session;

[Table("UserSession")]
public class UserSession
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