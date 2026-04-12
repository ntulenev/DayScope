using System.Windows;
using System.Windows.Controls;

using DayScope.ViewModels;

namespace DayScope.Views;

/// <summary>
/// Calculates viewport metrics for the main window schedule surface.
/// </summary>
internal static class MainWindowViewportController
{
    private const double NOW_LINE_TOP_MARGIN = 280d;
    private const double SCHEDULE_SURFACE_PADDING = 18d;
    private const double WINDOW_FALLBACK_OFFSET = 280d;

    /// <summary>
    /// Recalculates the available width for the schedule surface.
    /// </summary>
    /// <param name="viewModel">The dashboard view model.</param>
    /// <param name="scheduleSurfaceBorder">The border that hosts the schedule surface.</param>
    /// <param name="windowActualWidth">The current window width.</param>
    public static void UpdateAvailableScheduleWidth(
        MainWindowViewModel viewModel,
        FrameworkElement scheduleSurfaceBorder,
        double windowActualWidth)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(scheduleSurfaceBorder);

        var availableWidth = scheduleSurfaceBorder.ActualWidth > 0
            ? scheduleSurfaceBorder.ActualWidth - SCHEDULE_SURFACE_PADDING
            : windowActualWidth - WINDOW_FALLBACK_OFFSET;

        viewModel.UpdateAvailableScheduleWidth(availableWidth);
    }

    /// <summary>
    /// Scrolls the schedule so the current-time marker remains visible.
    /// </summary>
    /// <param name="viewModel">The dashboard view model.</param>
    /// <param name="scheduleScrollViewer">The scroll viewer that hosts the schedule timeline.</param>
    public static void ScrollToNowLine(
        MainWindowViewModel viewModel,
        ScrollViewer scheduleScrollViewer)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(scheduleScrollViewer);

        var targetOffset = Math.Max(0, viewModel.Schedule.NowLineTop - NOW_LINE_TOP_MARGIN);
        SmoothScrollBehavior.ScrollToOffset(scheduleScrollViewer, targetOffset);
    }
}
