using DayScope.Application.Abstractions;

namespace DayScope.Infrastructure.Clock;

public sealed class SystemClockService : IClockService
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
