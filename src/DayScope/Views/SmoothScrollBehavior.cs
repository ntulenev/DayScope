using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    private static readonly DependencyProperty _scrollAnimatorProperty =
        DependencyProperty.RegisterAttached(
            "ScrollAnimator",
            typeof(SmoothScrollAnimator),
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

        targetOffset = Math.Clamp(targetOffset, 0, scrollViewer.ScrollableHeight);
        if (!GetIsEnabled(scrollViewer))
        {
            scrollViewer.ScrollToVerticalOffset(targetOffset);
            return;
        }

        GetOrCreateAnimator(scrollViewer).ScrollToOffset(targetOffset);
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
            GetOrCreateAnimator(scrollViewer).SyncToViewer();
            return;
        }

        scrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;
        scrollViewer.ScrollChanged -= OnScrollChanged;
        scrollViewer.Unloaded -= OnUnloaded;

        if (scrollViewer.GetValue(_scrollAnimatorProperty) is SmoothScrollAnimator animator)
        {
            animator.Stop();
            scrollViewer.ClearValue(_scrollAnimatorProperty);
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer || scrollViewer.ScrollableHeight <= 0)
        {
            return;
        }

        var offsetChange = -(e.Delta / MOUSE_WHEEL_DELTA_PER_STEP) * GetWheelStep(scrollViewer);
        if (!GetOrCreateAnimator(scrollViewer).TryScrollBy(offsetChange, MINIMUM_OFFSET_CHANGE))
        {
            return;
        }

        e.Handled = true;
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer ||
            scrollViewer.GetValue(_scrollAnimatorProperty) is not SmoothScrollAnimator animator ||
            animator.IsApplyingScroll)
        {
            return;
        }

        if (Math.Abs(e.VerticalChange) < double.Epsilon &&
            Math.Abs(e.ExtentHeightChange) < double.Epsilon &&
            Math.Abs(e.ViewportHeightChange) < double.Epsilon)
        {
            return;
        }

        animator.SyncToViewer();
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer &&
            scrollViewer.GetValue(_scrollAnimatorProperty) is SmoothScrollAnimator animator)
        {
            animator.Stop();
        }
    }

    private static SmoothScrollAnimator GetOrCreateAnimator(ScrollViewer scrollViewer)
    {
        if (scrollViewer.GetValue(_scrollAnimatorProperty) is SmoothScrollAnimator existingAnimator)
        {
            return existingAnimator;
        }

        var newAnimator = new SmoothScrollAnimator(
            scrollViewer,
            () => GetAnimationDuration(scrollViewer));
        scrollViewer.SetValue(_scrollAnimatorProperty, newAnimator);
        return newAnimator;
    }

    private const double MOUSE_WHEEL_DELTA_PER_STEP = 120d;
    private const double MINIMUM_OFFSET_CHANGE = 0.1d;
}
