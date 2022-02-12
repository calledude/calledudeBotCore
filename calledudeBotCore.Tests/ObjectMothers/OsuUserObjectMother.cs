using calledudeBot.Models;

namespace calledudeBotCore.Tests.ObjectMothers;

public static class OsuUserObjectMother
{
    public static OsuUser CreateOsuUser()
        => new("calledude", 42069, "100.12", 4141.41f, 99.95f, 1337, null);
}
