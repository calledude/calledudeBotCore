using calledudeBot.Models;
using System.Collections.Generic;

namespace calledudeBotCore.Tests.ObjectMothers;

public static class OsuUserObjectMother
{
    public static OsuUser CreateOsuUser(
        string username = "calledude",
        int rank = 42069,
        string level = "100.12",
        float pp = 4141.41f,
        float accuracy = 99.95f,
        int countryRank = 1337,
        List<object> events = null)
        => new(username, rank, level, pp, accuracy, countryRank, events);
}
