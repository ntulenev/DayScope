using System.Windows.Threading;

namespace DayScope.Threading;

/// <summary>
/// Creates <see cref="DispatcherTimer"/>-backed timer abstractions for the WPF UI thread.
/// </summary>
public sealed class DispatcherTimerFactory : IUiDispatcherTimerFactory
{
    /// <summary>
    /// Creates a dispatcher-backed timer configured with the provided interval.
    /// </summary>
    /// <param name="interval">The initial timer interval.</param>
    /// <returns>A timer abstraction backed by <see cref="DispatcherTimer"/>.</returns>
    public IUiDispatcherTimer Create(TimeSpan interval) => new DispatcherTimerAdapter(interval);
}
