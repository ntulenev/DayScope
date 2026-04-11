namespace DayScope.Threading;

/// <summary>
/// Represents a UI-thread timer abstraction used by presentation logic.
/// </summary>
public interface IUiDispatcherTimer
{
    event EventHandler? Tick;

    TimeSpan Interval { get; set; }

    /// <summary>
    /// Starts the timer.
    /// </summary>
    void StartTimer();

    /// <summary>
    /// Stops the timer.
    /// </summary>
    void StopTimer();
}
