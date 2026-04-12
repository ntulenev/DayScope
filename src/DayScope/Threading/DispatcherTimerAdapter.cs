using System.Windows.Threading;

namespace DayScope.Threading;

/// <summary>
/// Adapts <see cref="DispatcherTimer"/> to <see cref="IUiDispatcherTimer"/>.
/// </summary>
internal sealed class DispatcherTimerAdapter : IUiDispatcherTimer
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
