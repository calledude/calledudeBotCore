using calledudeBot.Chat;
using MediatR;
using System;

namespace calledudeBot.Models;

public class UserParticipationNotification : INotification
{
    public User User { get; }
    public DateTime When { get; set; }
    public ParticipationType ParticipationType { get; }

    public UserParticipationNotification(User user, ParticipationType participationType)
    {
        User = user;
        When = DateTime.Now;
        ParticipationType = participationType;
    }
}
