namespace DayScope.Application.Abstractions;

/// <summary>
/// Exposes the current time.
/// </summary>
public interface IClockService
{
    DateTimeOffset Now { get; }
}
