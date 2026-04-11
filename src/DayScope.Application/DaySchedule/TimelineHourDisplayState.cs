namespace DayScope.Application.DaySchedule;

/// <summary>
/// Represents a single hour marker shown in a time column.
/// </summary>
/// <param name="Text">The formatted time label.</param>
/// <param name="Top">The top offset for the label.</param>
public sealed record TimelineHourDisplayState(string Text, double Top);
