namespace DayScope.Threading;

/// <summary>
/// Creates UI-thread timer instances for presentation components.
/// </summary>
public interface IUiDispatcherTimerFactory
{
    /// <summary>
    /// Creates a timer configured with the provided interval.
    /// </summary>
    /// <param name="interval">The initial timer interval.</param>
    /// <returns>A timer abstraction bound to the UI dispatcher.</returns>
    IUiDispatcherTimer Create(TimeSpan interval);
}
