using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

using DayScope.Application.DaySchedule;
using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Configuration;
using DayScope.Platform;
using DayScope.Themes;
using DayScope.ViewModels;

using Microsoft.Extensions.Options;

namespace DayScope.Views;

/// <summary>
/// Hosts the main DayScope dashboard window and its window-specific interactions.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowShellController _shellController;
    private readonly MainWindowThemeController _themeController;

    /// <summary>
    /// Gets the theme options rendered in the header menu.
    /// </summary>
    public ObservableCollection<MainWindowThemeOptionViewModel> ThemeMenuOptions { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The dashboard view model bound to the window.</param>
    /// <param name="themeManager">The theme manager used to update window chrome.</param>
    /// <param name="windowOptions">The configured window sizing options.</param>
    /// <param name="uriLauncher">The service used to open external links.</param>
    /// <param name="folderLauncher">The service used to open local folders.</param>
    /// <param name="clipboardService">The service used to copy text to the clipboard.</param>
    /// <param name="windowChromeController">The controller used to update native window chrome.</param>
    /// <param name="googleCalendarOptions">The configured Google integration options.</param>
    /// <param name="pathResolver">The service used to resolve configured local paths.</param>
    public MainWindow(
        MainWindowViewModel viewModel,
        ThemeManager themeManager,
        IOptions<WindowSettings> windowOptions,
        IUriLauncher uriLauncher,
        IFolderLauncher folderLauncher,
        IClipboardService clipboardService,
        IWindowChromeController windowChromeController,
        IOptions<GoogleCalendarSettings> googleCalendarOptions,
        IPathResolver pathResolver)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(themeManager);
        ArgumentNullException.ThrowIfNull(windowOptions);
        ArgumentNullException.ThrowIfNull(uriLauncher);
        ArgumentNullException.ThrowIfNull(folderLauncher);
        ArgumentNullException.ThrowIfNull(clipboardService);
        ArgumentNullException.ThrowIfNull(windowChromeController);
        ArgumentNullException.ThrowIfNull(googleCalendarOptions);
        ArgumentNullException.ThrowIfNull(pathResolver);

        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _uriLauncher = uriLauncher;
        _folderLauncher = folderLauncher;
        _clipboardService = clipboardService;
        _themeManager = themeManager;
        _googleCalendarSettings = googleCalendarOptions.Value;
        _pathResolver = pathResolver;
        _shellController = new MainWindowShellController();
        _themeController = new MainWindowThemeController(themeManager, windowChromeController);

        foreach (var option in AppThemeOptions.All)
        {
            ThemeMenuOptions.Add(new MainWindowThemeOptionViewModel(option.Mode, option.Label));
        }

        UpdateThemeMenuSelection();

        MainWindowShellController.ApplyWindowSettings(this, windowOptions.Value);
        _themeController.Attach(this);
        _themeManager.ThemeChanged += OnThemeChanged;
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
    public void ShowFromTray() => MainWindowShellController.ShowFromTray(this, OnShownFromTray);

    /// <summary>
    /// Hides the window to the system tray.
    /// </summary>
    public void HideToTray() => MainWindowShellController.HideToTray(this);

    /// <summary>
    /// Requests an immediate dashboard refresh.
    /// </summary>
    /// <returns>A task that completes when the refresh has finished.</returns>
    public Task RefreshNowAsync() => _viewModel.RefreshNowAsync();

    /// <summary>
    /// Copies the active day's schedule summary to the clipboard.
    /// </summary>
    /// <returns><see langword="true"/> when the text was copied; otherwise <see langword="false"/>.</returns>
    public bool CopyScheduleToClipboard() =>
        _clipboardService.TrySetText(ScheduleClipboardTextBuilder.Build(_viewModel.Schedule));

    /// <summary>
    /// Allows the window to close instead of minimizing to the tray.
    /// </summary>
    public void CloseFromTray() => _shellController.CloseFromTray(this);

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
    private void OnCloseEventDetailsClick(object sender, RoutedEventArgs e) => _viewModel.CloseEventDetails();

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
    private void OnOpenUnreadEmailsClick(object sender, RoutedEventArgs e) => _uriLauncher.Open(_viewModel.Inbox.UnreadEmailInboxUri);

    /// <summary>
    /// Toggles the header settings menu anchored next to the unread-email shortcut.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnToggleHeaderMenuClick(object sender, RoutedEventArgs e)
    {
        HeaderMenuPopup.IsOpen = !HeaderMenuPopup.IsOpen;
        if (!HeaderMenuPopup.IsOpen)
        {
            CollapseThemeMenu();
        }
    }

    /// <summary>
    /// Collapses the nested theme section after the popup closes.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnHeaderMenuClosed(object? sender, EventArgs e) => CollapseThemeMenu();

    /// <summary>
    /// Shows or hides the theme section inside the header menu.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnToggleThemeMenuClick(object sender, RoutedEventArgs e)
    {
        CollapseOpenFolderMenu();
        ThemeMenuItemsPanel.Visibility = ThemeMenuItemsPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    /// <summary>
    /// Shows or hides the local-folder section inside the header menu.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnToggleOpenFolderMenuClick(object sender, RoutedEventArgs e)
    {
        CollapseThemeMenu();
        OpenFolderMenuItemsPanel.Visibility = OpenFolderMenuItemsPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    /// <summary>
    /// Toggles whether the configured secondary time zone is shown in the schedule.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnToggleSecondaryTimeZoneClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.ToggleShowSecondaryTimeZone())
        {
            return;
        }

        CloseHeaderMenu();
        UpdateLayout();
        UpdateScheduleWidth();
    }

    /// <summary>
    /// Triggers an immediate refresh from the in-window settings menu.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private async void OnRefreshFromHeaderMenuClickAsync(object sender, RoutedEventArgs e)
    {
        CloseHeaderMenu();
        await RefreshNowAsync();
    }

    /// <summary>
    /// Copies the active day's schedule summary from the in-window settings menu.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnCopyScheduleFromHeaderMenuClick(object sender, RoutedEventArgs e)
    {
        CloseHeaderMenu();
        _ = CopyScheduleToClipboard();
    }

    /// <summary>
    /// Opens the folder containing the running application.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnOpenApplicationFolderClick(object sender, RoutedEventArgs e)
    {
        CloseHeaderMenu();
        _folderLauncher.Open(AppContext.BaseDirectory);
    }

    /// <summary>
    /// Opens the folder containing the configured Google credentials file.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnOpenCredentialsFolderClick(object sender, RoutedEventArgs e)
    {
        CloseHeaderMenu();
        _folderLauncher.Open(GetCredentialsFolderPath());
    }

    /// <summary>
    /// Hides the main window to the tray from the in-window settings menu.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnHideToTrayClick(object sender, RoutedEventArgs e)
    {
        CloseHeaderMenu();
        HideToTray();
    }

    /// <summary>
    /// Applies the selected theme from the in-window settings menu.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnThemeModeMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: MainWindowThemeOptionViewModel option })
        {
            return;
        }

        _themeManager.SetThemeMode(option.Mode);
        CloseHeaderMenu();
    }

    /// <summary>
    /// Exits the application from the in-window settings menu.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnExitFromHeaderMenuClick(object sender, RoutedEventArgs e)
    {
        CloseHeaderMenu();
        CloseFromTray();
        System.Windows.Application.Current?.Shutdown();
    }

    /// <summary>
    /// Opens Google Calendar for the currently selected day.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnOpenGoogleCalendarClick(object sender, RoutedEventArgs e) => _uriLauncher.Open(_viewModel.Inbox.GoogleCalendarUri);

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
    /// Copies the selected event title, time, and description to the clipboard.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnCopyEventDetailsClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.EventDetails.SelectedEventDetails is not { } eventDetails)
        {
            return;
        }

        _ = _clipboardService.TrySetText(
            BuildEventDetailsClipboardText(
                eventDetails,
                _viewModel.Schedule.PrimaryTimeZoneLabel));
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
    private void ScrollScheduleToNowLine() => MainWindowViewportController.ScrollToNowLine(_viewModel, ScheduleScrollViewer);

    /// <summary>
    /// Hides the window to the tray unless tray-driven close is allowed.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The cancel event arguments.</param>
    private void OnClosing(object? sender, CancelEventArgs e) => _shellController.HandleClosing(this, e);

    /// <summary>
    /// Recalculates layout-dependent viewport values after the window size changes.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateScheduleWidth();

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
        _themeManager.ThemeChanged -= OnThemeChanged;
        _themeController.Detach(this);
        _viewModel.Dispose();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(UpdateThemeMenuSelection);
            return;
        }

        UpdateThemeMenuSelection();
    }

    private void UpdateThemeMenuSelection()
    {
        foreach (var option in ThemeMenuOptions)
        {
            option.IsSelected = option.Mode == _themeManager.SelectedMode;
        }
    }

    private void CloseHeaderMenu()
    {
        HeaderMenuPopup.IsOpen = false;
        CollapseThemeMenu();
        CollapseOpenFolderMenu();
    }

    private void CollapseThemeMenu() => ThemeMenuItemsPanel.Visibility = Visibility.Collapsed;

    private void CollapseOpenFolderMenu() => OpenFolderMenuItemsPanel.Visibility = Visibility.Collapsed;

    private string GetCredentialsFolderPath()
    {
        var credentialsPath = _pathResolver.ResolvePath(_googleCalendarSettings.ClientSecretsPath);
        return Path.GetDirectoryName(credentialsPath) ?? AppContext.BaseDirectory;
    }

    private static string BuildEventDetailsClipboardText(
        EventDetailsDisplayState eventDetails,
        string? timeZoneLabel)
    {
        var builder = new StringBuilder();
        builder.Append("Title: ").AppendLine(eventDetails.Title);
        builder.AppendLine();
        builder.Append("Time: ").AppendLine(FormatClipboardScheduleText(eventDetails.ScheduleText, timeZoneLabel));

        var description = HtmlTextBlockRenderer.ToPlainText(eventDetails.Description);
        builder.AppendLine();
        builder.AppendLine("Description:");
        builder.AppendLine(string.IsNullOrWhiteSpace(description) ? "No description" : description);
        return builder.ToString().TrimEnd();
    }

    private static string FormatClipboardScheduleText(string scheduleText, string? timeZoneLabel)
    {
        return string.IsNullOrWhiteSpace(timeZoneLabel)
            ? scheduleText
            : string.Concat(scheduleText, " (", timeZoneLabel.Trim(), ")");
    }

    private readonly MainWindowViewModel _viewModel;
    private readonly IUriLauncher _uriLauncher;
    private readonly IFolderLauncher _folderLauncher;
    private readonly IClipboardService _clipboardService;
    private readonly ThemeManager _themeManager;
    private readonly GoogleCalendarSettings _googleCalendarSettings;
    private readonly IPathResolver _pathResolver;
}
