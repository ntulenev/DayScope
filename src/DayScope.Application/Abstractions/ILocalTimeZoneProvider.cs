namespace DayScope.Application.Abstractions;

/// <summary>
/// Exposes the local time zone used for display calculations.
/// </summary>
public interface ILocalTimeZoneProvider
{
    TimeZoneInfo LocalTimeZone { get; }
}
