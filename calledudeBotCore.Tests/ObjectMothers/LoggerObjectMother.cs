using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace calledudeBotCore.Tests.ObjectMothers;

public static class LoggerObjectMother
{
    public static Logger<T> NullLoggerFor<T>()
        => new(NullLoggerFactory.Instance);
}
