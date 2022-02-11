using calledudeBot.Models;

namespace calledudeBotCore.Tests.ObjectMothers;

public static class OsuUserObjectMother
{
    public static OsuUser GetOsuUser()
        => new()
        {
            Accuracy = 99.95f,
            CountryRank = 1337,
            Level = "100.12",
            PP = 4141.41f,
            Rank = 42069,
            Username = "calledude"
        };
}
