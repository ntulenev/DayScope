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

    /// <summary>
    /// Adapts <see cref="DispatcherTimer"/> to <see cref="IUiDispatcherTimer"/>.
    /// </summary>
    private sealed class DispatcherTimerAdapter : IUiDispatcherTimer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherTimerAdapter"/> class.
        /// </summary>
        /// <param name="interval">The initial timer interval.</param>
        public DispatcherTimerAdapter(TimeSpan interval)
        {
            _timer.Interval = interval;
        }

        public event EventHandler? Tick {
            add => _timer.Tick += value;

            remove => _timer.Tick -= value;
        }

        public TimeSpan Interval {
            get => _timer.Interval;

            set => _timer.Interval = value;
        }

        /// <summary>
        /// Starts the underlying dispatcher timer.
        /// </summary>
        public void StartTimer() => _timer.Start();

        /// <summary>
        /// Stops the underlying dispatcher timer.
        /// </summary>
        public void StopTimer() => _timer.Stop();

        private readonly DispatcherTimer _timer = new();
    }
}
