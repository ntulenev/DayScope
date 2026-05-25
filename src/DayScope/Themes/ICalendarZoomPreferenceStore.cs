namespace DayScope.Themes;

/// <summary>
/// Persists and restores the calendar body zoom used in the schedule UI.
/// </summary>
public interface ICalendarZoomPreferenceStore
{
    /// <summary>
    /// Loads the persisted calendar zoom scale.
    /// </summary>
    /// <returns>The saved zoom scale, or 1 when no preference is available.</returns>
    double LoadCalendarZoomScale();

    /// <summary>
    /// Saves the calendar zoom scale.
    /// </summary>
    /// <param name="calendarZoomScale">The calendar zoom scale to persist.</param>
    void SaveCalendarZoomScale(double calendarZoomScale);
}
