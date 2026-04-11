using DayScope.Application.Abstractions;

namespace DayScope.Infrastructure.Clock;

/// <summary>
/// Reads the current time from the local system clock.
/// </summary>
public sealed class SystemClockService : IClockService
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
