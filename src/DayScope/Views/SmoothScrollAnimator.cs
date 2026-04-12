using System.Windows.Controls;
using System.Windows.Threading;

namespace DayScope.Views;

/// <summary>
/// Animates one <see cref="ScrollViewer"/> toward a target vertical offset.
/// </summary>
internal sealed class SmoothScrollAnimator
{
    private readonly ScrollViewer _scrollViewer;
    private readonly Func<TimeSpan> _getAnimationDuration;
    private readonly DispatcherTimer _timer;
    private DateTime _animationStartedAtUtc;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmoothScrollAnimator"/> class.
    /// </summary>
    /// <param name="scrollViewer">The associated scroll viewer.</param>
    /// <param name="getAnimationDuration">The delegate used to read the current animation duration.</param>
    public SmoothScrollAnimator(
        ScrollViewer scrollViewer,
        Func<TimeSpan> getAnimationDuration)
    {
        ArgumentNullException.ThrowIfNull(scrollViewer);
        ArgumentNullException.ThrowIfNull(getAnimationDuration);

        _scrollViewer = scrollViewer;
        _getAnimationDuration = getAnimationDuration;
        _timer = new DispatcherTimer(DispatcherPriority.Render, scrollViewer.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

        _timer.Tick += OnTick;
        StartOffset = scrollViewer.VerticalOffset;
        TargetOffset = scrollViewer.VerticalOffset;
    }

    /// <summary>
    /// Gets a value indicating whether the animator is currently applying a scroll update.
    /// </summary>
    public bool IsApplyingScroll { get; private set; }

    /// <summary>
    /// Gets the current animation target offset.
    /// </summary>
    public double TargetOffset { get; private set; }

    private double StartOffset { get; set; }

    /// <summary>
    /// Attempts to scroll by the provided offset delta.
    /// </summary>
    /// <param name="offsetChange">The requested offset delta.</param>
    /// <param name="minimumOffsetChange">The minimum meaningful delta for a new animation.</param>
    /// <returns><see langword="true"/> when a new scroll animation was started; otherwise, <see langword="false"/>.</returns>
    public bool TryScrollBy(double offsetChange, double minimumOffsetChange)
    {
        var targetOffset = ClampOffset(TargetOffset + offsetChange);
        if (Math.Abs(targetOffset - TargetOffset) < minimumOffsetChange &&
            Math.Abs(targetOffset - _scrollViewer.VerticalOffset) < minimumOffsetChange)
        {
            return false;
        }

        ScrollToOffset(targetOffset);
        return true;
    }

    /// <summary>
    /// Starts animating toward the requested offset.
    /// </summary>
    /// <param name="targetOffset">The destination offset.</param>
    public void ScrollToOffset(double targetOffset)
    {
        StartOffset = _scrollViewer.VerticalOffset;
        TargetOffset = ClampOffset(targetOffset);
        Start();
    }

    /// <summary>
    /// Resynchronizes the animator with the viewer's current scroll position.
    /// </summary>
    public void SyncToViewer()
    {
        Stop();
        StartOffset = _scrollViewer.VerticalOffset;
        TargetOffset = _scrollViewer.VerticalOffset;
    }

    /// <summary>
    /// Stops the active animation if one is running.
    /// </summary>
    public void Stop()
    {
        _timer.Stop();
    }

    private void Start()
    {
        _animationStartedAtUtc = DateTime.UtcNow;
        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var duration = _getAnimationDuration();
        if (duration <= TimeSpan.Zero)
        {
            ApplyOffset(TargetOffset);
            Stop();
            return;
        }

        var elapsed = DateTime.UtcNow - _animationStartedAtUtc;
        var progress = Math.Clamp(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 0d, 1d);
        var easedProgress = EaseOutCubic(progress);
        var currentOffset = StartOffset + ((TargetOffset - StartOffset) * easedProgress);

        ApplyOffset(currentOffset);
        if (progress < 1d)
        {
            return;
        }

        StartOffset = TargetOffset;
        Stop();
    }

    private void ApplyOffset(double offset)
    {
        IsApplyingScroll = true;
        _scrollViewer.ScrollToVerticalOffset(offset);
        IsApplyingScroll = false;
    }

    private double ClampOffset(double offset)
    {
        return Math.Clamp(offset, 0, _scrollViewer.ScrollableHeight);
    }

    private static double EaseOutCubic(double progress)
    {
        return 1 - Math.Pow(1 - progress, 3);
    }
}
