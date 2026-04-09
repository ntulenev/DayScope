namespace DayScope.Application.Abstractions;

public interface IClockService
{
    DateTimeOffset Now { get; }
}
