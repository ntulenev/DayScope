using System.Windows;
using System.Windows.Input;
using System.ComponentModel;

using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;
using DayScope.Platform;
using DayScope.Themes;
using DayScope.ViewModels;

namespace DayScope.Views;

/// <summary>
/// Hosts the main DayScope dashboard window and its window-specific interactions.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowShellController _shellController;
    private readonly MainWindowThemeController _themeController;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The dashboard view model bound to the window.</param>
    /// <param name="themeManager">The theme manager used to update window chrome.</param>
    /// <param name="windowOptions">The configured window sizing options.</param>
    /// <param name="uriLauncher">The service used to open external links.</param>
    /// <param name="clipboardService">The service used to copy text to the clipboard.</param>
    /// <param name="windowChromeController">The controller used to update native window chrome.</param>
    public MainWindow(
        MainWindowViewModel viewModel,
        ThemeManager themeManager,
        IOptions<WindowSettings> windowOptions,
        IUriLauncher uriLauncher,
        IClipboardService clipboardService,
        IWindowChromeController windowChromeController)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(themeManager);
        ArgumentNullException.ThrowIfNull(windowOptions);
        ArgumentNullException.ThrowIfNull(uriLauncher);
        ArgumentNullException.ThrowIfNull(clipboardService);
        ArgumentNullException.ThrowIfNull(windowChromeController);

        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _uriLauncher = uriLauncher;
        _clipboardService = clipboardService;
        _shellController = new MainWindowShellController();
        _themeController = new MainWindowThemeController(themeManager, windowChromeController);

        MainWindowShellController.ApplyWindowSettings(this, windowOptions.Value);
        _themeController.Attach(this);
        SizeChanged += OnSizeChanged;
        Closing += OnClosing;
        Closed += OnClosed;
    }

    /// <summary>
    /// Performs initial window sizing and loads dashboard data.
    /// </summary>
    /// <returns>A task that completes when the window is initialized.</returns>
    public async Task InitializeAsync()
    {
        UpdateScheduleWidth();
        await _viewModel.InitializeAsync();
    }

    /// <summary>
    /// Shows the window from the tray and restores focus to it.
    /// </summary>
    public void ShowFromTray()
    {
        MainWindowShellController.ShowFromTray(this, OnShownFromTray);
    }

    /// <summary>
    /// Hides the window to the system tray.
    /// </summary>
    public void HideToTray()
    {
        MainWindowShellController.HideToTray(this);
    }

    /// <summary>
    /// Requests an immediate dashboard refresh.
    /// </summary>
    /// <returns>A task that completes when the refresh has finished.</returns>
    public Task RefreshNowAsync() => _viewModel.RefreshNowAsync();

    /// <summary>
    /// Allows the window to close instead of minimizing to the tray.
    /// </summary>
    public void CloseFromTray()
    {
        _shellController.CloseFromTray(this);
    }

    /// <summary>
    /// Opens details when the user clicks an event card.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void OnEventCardMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        _viewModel.OpenEventDetails(element.DataContext);
        e.Handled = true;
    }

    /// <summary>
    /// Closes the details overlay when the background is clicked.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void OnEventDetailsOverlayMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _viewModel.CloseEventDetails();
        e.Handled = true;
    }

    /// <summary>
    /// Closes the event details overlay.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnCloseEventDetailsClick(object sender, RoutedEventArgs e)
    {
        _viewModel.CloseEventDetails();
    }

    /// <summary>
    /// Opens the selected event link in the default browser.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnOpenEventLinkClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.EventDetails.SelectedEventDetails?.JoinUrl is not Uri joinUrl)
        {
            return;
        }

        _uriLauncher.Open(joinUrl);
    }

    /// <summary>
    /// Opens Gmail for the current inbox context.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnOpenUnreadEmailsClick(object sender, RoutedEventArgs e)
    {
        _uriLauncher.Open(_viewModel.Inbox.UnreadEmailInboxUri);
    }

    /// <summary>
    /// Opens Google Calendar for the currently selected day.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnOpenGoogleCalendarClick(object sender, RoutedEventArgs e)
    {
        _uriLauncher.Open(_viewModel.Inbox.GoogleCalendarUri);
    }

    /// <summary>
    /// Navigates to the previous day.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private async void OnPreviousDayClickAsync(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateDaysAsync(-1);
        ScrollScheduleToNowLine();
    }

    /// <summary>
    /// Navigates to the next day.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private async void OnNextDayClickAsync(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateDaysAsync(1);
        ScrollScheduleToNowLine();
    }

    /// <summary>
    /// Copies the selected event link to the clipboard.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnCopyEventLinkClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.EventDetails.SelectedEventDetails?.JoinUrl is not Uri joinUrl)
        {
            return;
        }

        _ = _clipboardService.TrySetText(joinUrl.AbsoluteUri);
    }

    /// <summary>
    /// Recalculates the available width for the schedule surface.
    /// </summary>
    private void UpdateScheduleWidth()
    {
        MainWindowViewportController.UpdateAvailableScheduleWidth(
            _viewModel,
            ScheduleSurfaceBorder,
            ActualWidth);
    }

    /// <summary>
    /// Scrolls the schedule so the current-time marker is visible.
    /// </summary>
    private void ScrollScheduleToNowLine()
    {
        MainWindowViewportController.ScrollToNowLine(_viewModel, ScheduleScrollViewer);
    }

    /// <summary>
    /// Hides the window to the tray unless tray-driven close is allowed.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The cancel event arguments.</param>
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _shellController.HandleClosing(this, e);
    }

    /// <summary>
    /// Recalculates layout-dependent viewport values after the window size changes.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateScheduleWidth();
    }

    /// <summary>
    /// Applies post-show layout updates after the tray restores the main window.
    /// </summary>
    private void OnShownFromTray()
    {
        UpdateLayout();
        UpdateScheduleWidth();
        ScrollScheduleToNowLine();
    }

    /// <summary>
    /// Releases window-scoped controllers and the bound view model when the window closes.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnClosed(object? sender, EventArgs e)
    {
        _themeController.Detach(this);
        _viewModel.Dispose();
    }

    private readonly MainWindowViewModel _viewModel;
    private readonly IUriLauncher _uriLauncher;
    private readonly IClipboardService _clipboardService;
}
