using DayScope.Application.Abstractions;

namespace DayScope.Infrastructure.Clock;

/// <summary>
/// Reads the local time zone from the operating system.
/// </summary>
public sealed class SystemTimeZoneProvider : ILocalTimeZoneProvider
{
    public TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;
}
