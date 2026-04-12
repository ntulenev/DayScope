using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace DayScope.Views;

/// <summary>
/// Adds smooth mouse-wheel scrolling to <see cref="ScrollViewer"/> instances.
/// </summary>
public static class SmoothScrollBehavior
{
    /// <summary>
    /// Identifies whether smooth scrolling is enabled for the target <see cref="ScrollViewer"/>.
    /// </summary>
    private static readonly DependencyProperty _isEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SmoothScrollBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    /// <summary>
    /// Identifies the number of pixels to travel for one mouse-wheel notch.
    /// </summary>
    private static readonly DependencyProperty _wheelStepProperty =
        DependencyProperty.RegisterAttached(
            "WheelStep",
            typeof(double),
            typeof(SmoothScrollBehavior),
            new PropertyMetadata(88.1d));

    /// <summary>
    /// Identifies the duration used for each smooth-scroll animation.
    /// </summary>
    private static readonly DependencyProperty _animationDurationProperty =
        DependencyProperty.RegisterAttached(
            "AnimationDuration",
            typeof(TimeSpan),
            typeof(SmoothScrollBehavior),
            new PropertyMetadata(TimeSpan.FromMilliseconds(220)));

    private static readonly DependencyProperty _scrollStateProperty =
        DependencyProperty.RegisterAttached(
            "ScrollState",
            typeof(ScrollState),
            typeof(SmoothScrollBehavior),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets whether smooth scrolling is enabled.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <returns><see langword="true"/> when enabled; otherwise, <see langword="false"/>.</returns>
    public static bool GetIsEnabled(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return (bool)element.GetValue(_isEnabledProperty);
    }

    /// <summary>
    /// Sets whether smooth scrolling is enabled.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <param name="value">Whether smooth scrolling should be enabled.</param>
    public static void SetIsEnabled(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);

        element.SetValue(_isEnabledProperty, value);
    }

    /// <summary>
    /// Gets the configured wheel step in pixels.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <returns>The configured wheel step.</returns>
    public static double GetWheelStep(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return (double)element.GetValue(_wheelStepProperty);
    }

    /// <summary>
    /// Sets the configured wheel step in pixels.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <param name="value">The wheel step in pixels.</param>
    public static void SetWheelStep(DependencyObject element, double value)
    {
        ArgumentNullException.ThrowIfNull(element);

        element.SetValue(_wheelStepProperty, value);
    }

    /// <summary>
    /// Gets the configured animation duration.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <returns>The configured animation duration.</returns>
    public static TimeSpan GetAnimationDuration(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return (TimeSpan)element.GetValue(_animationDurationProperty);
    }

    /// <summary>
    /// Sets the configured animation duration.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <param name="value">The animation duration.</param>
    public static void SetAnimationDuration(DependencyObject element, TimeSpan value)
    {
        ArgumentNullException.ThrowIfNull(element);

        element.SetValue(_animationDurationProperty, value);
    }

    /// <summary>
    /// Scrolls the viewer to the provided vertical offset using the configured smooth animation.
    /// </summary>
    /// <param name="scrollViewer">The scroll viewer to animate.</param>
    /// <param name="targetOffset">The destination vertical offset.</param>
    public static void ScrollToOffset(ScrollViewer scrollViewer, double targetOffset)
    {
        ArgumentNullException.ThrowIfNull(scrollViewer);

        targetOffset = ClampOffset(scrollViewer, targetOffset);
        if (!GetIsEnabled(scrollViewer))
        {
            scrollViewer.ScrollToVerticalOffset(targetOffset);
            return;
        }

        var state = GetOrCreateState(scrollViewer);
        StartAnimation(scrollViewer, state, targetOffset);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scrollViewer)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
            scrollViewer.ScrollChanged += OnScrollChanged;
            scrollViewer.Unloaded += OnUnloaded;
            SyncState(scrollViewer, GetOrCreateState(scrollViewer));
            return;
        }

        scrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;
        scrollViewer.ScrollChanged -= OnScrollChanged;
        scrollViewer.Unloaded -= OnUnloaded;

        if (scrollViewer.GetValue(_scrollStateProperty) is ScrollState state)
        {
            state.Stop();
            scrollViewer.ClearValue(_scrollStateProperty);
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer || scrollViewer.ScrollableHeight <= 0)
        {
            return;
        }

        var state = GetOrCreateState(scrollViewer);
        var offsetChange = -(e.Delta / MOUSE_WHEEL_DELTA_PER_STEP) * GetWheelStep(scrollViewer);
        var targetOffset = ClampOffset(scrollViewer, state.TargetOffset + offsetChange);
        if (Math.Abs(targetOffset - state.TargetOffset) < MINIMUM_OFFSET_CHANGE &&
            Math.Abs(targetOffset - scrollViewer.VerticalOffset) < MINIMUM_OFFSET_CHANGE)
        {
            return;
        }

        StartAnimation(scrollViewer, state, targetOffset);
        e.Handled = true;
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer ||
            scrollViewer.GetValue(_scrollStateProperty) is not ScrollState state ||
            state.IsAnimatingScroll)
        {
            return;
        }

        if (Math.Abs(e.VerticalChange) < double.Epsilon &&
            Math.Abs(e.ExtentHeightChange) < double.Epsilon &&
            Math.Abs(e.ViewportHeightChange) < double.Epsilon)
        {
            return;
        }

        SyncState(scrollViewer, state);
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer &&
            scrollViewer.GetValue(_scrollStateProperty) is ScrollState state)
        {
            state.Stop();
        }
    }

    private static ScrollState GetOrCreateState(ScrollViewer scrollViewer)
    {
        if (scrollViewer.GetValue(_scrollStateProperty) is ScrollState existingState)
        {
            return existingState;
        }

        var newState = new ScrollState(scrollViewer);
        scrollViewer.SetValue(_scrollStateProperty, newState);
        return newState;
    }

    private static void StartAnimation(ScrollViewer scrollViewer, ScrollState state, double targetOffset)
    {
        state.StartOffset = scrollViewer.VerticalOffset;
        state.TargetOffset = ClampOffset(scrollViewer, targetOffset);
        state.Start();
    }

    private static void SyncState(ScrollViewer scrollViewer, ScrollState state)
    {
        state.Stop();
        state.StartOffset = scrollViewer.VerticalOffset;
        state.TargetOffset = scrollViewer.VerticalOffset;
    }

    private static double ClampOffset(ScrollViewer scrollViewer, double offset)
    {
        return Math.Clamp(offset, 0, scrollViewer.ScrollableHeight);
    }

    private static double EaseOutCubic(double progress)
    {
        return 1 - Math.Pow(1 - progress, 3);
    }

    private const double MOUSE_WHEEL_DELTA_PER_STEP = 120d;
    private const double MINIMUM_OFFSET_CHANGE = 0.1d;

    /// <summary>
    /// Stores smooth-scrolling state for one <see cref="ScrollViewer"/>.
    /// </summary>
    private sealed class ScrollState
    {
        private readonly ScrollViewer _scrollViewer;
        private readonly DispatcherTimer _timer;
        private DateTime _animationStartedAtUtc;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollState"/> class.
        /// </summary>
        /// <param name="scrollViewer">The associated scroll viewer.</param>
        public ScrollState(ScrollViewer scrollViewer)
        {
            _scrollViewer = scrollViewer;
            _timer = new DispatcherTimer(DispatcherPriority.Render, scrollViewer.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };

            _timer.Tick += OnTick;
            StartOffset = scrollViewer.VerticalOffset;
            TargetOffset = scrollViewer.VerticalOffset;
        }

        /// <summary>
        /// Gets or sets the animation start offset.
        /// </summary>
        public double StartOffset { get; set; }

        /// <summary>
        /// Gets or sets the target animation offset.
        /// </summary>
        public double TargetOffset { get; set; }

        /// <summary>
        /// Gets a value indicating whether the behavior is currently applying a scroll update.
        /// </summary>
        public bool IsAnimatingScroll { get; private set; }

        /// <summary>
        /// Starts the active animation from the current viewer offset.
        /// </summary>
        public void Start()
        {
            _animationStartedAtUtc = DateTime.UtcNow;
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
        }

        /// <summary>
        /// Stops the active animation if one is running.
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            var duration = GetAnimationDuration(_scrollViewer);
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
            IsAnimatingScroll = true;
            _scrollViewer.ScrollToVerticalOffset(offset);
            IsAnimatingScroll = false;
        }
    }
}
