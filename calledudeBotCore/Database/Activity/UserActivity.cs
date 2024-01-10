using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace calledudeBot.Database.Activity;

[Table("UserActivities")]
public class UserActivity
{
	[Required]
	[Key]
	public string? Username { get; set; }

	public int TimesSeen { get; set; }

	[Required]
	public DateTime LastJoinDate { get; set; }

	public int MessagesSent { get; set; }

	public Guid StreamSession { get; set; }
}