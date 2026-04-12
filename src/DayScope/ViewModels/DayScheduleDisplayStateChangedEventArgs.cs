using DayScope.Application.DaySchedule;

namespace DayScope.ViewModels;

/// <summary>
/// Carries an updated day-schedule display state.
/// </summary>
public sealed class DayScheduleDisplayStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DayScheduleDisplayStateChangedEventArgs"/> class.
    /// </summary>
    /// <param name="displayState">The updated display state.</param>
    public DayScheduleDisplayStateChangedEventArgs(DayScheduleDisplayState displayState)
    {
        DisplayState = displayState ?? throw new ArgumentNullException(nameof(displayState));
    }

    /// <summary>
    /// Gets the updated display state.
    /// </summary>
    public DayScheduleDisplayState DisplayState { get; }
}
