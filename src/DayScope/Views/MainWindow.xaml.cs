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
using Microsoft.Win32;

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
        Activated += OnActivatedAsync;
        Closing += OnClosing;
        Closed += OnClosed;
        SystemEvents.PowerModeChanged += OnPowerModeChangedAsync;
        SystemEvents.SessionSwitch += OnSessionSwitchAsync;
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
    /// Applies main-window keyboard shortcuts before focused child controls consume them.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The key event arguments.</param>
    private void OnMainWindowPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var shortcutKey = GetShortcutKey(e);
        if (PrivacyModeKeyboardShortcut.IsToggle(shortcutKey, Keyboard.Modifiers))
        {
            _viewModel.TogglePrivacyMode();
            e.Handled = true;
            return;
        }

        if (!CalendarZoomKeyboardShortcut.TryResolve(shortcutKey, Keyboard.Modifiers, out var action))
        {
            return;
        }

        ChangeCalendarZoom(action);
        e.Handled = true;
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
    /// Toggles privacy mode from the in-window settings menu.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnTogglePrivacyModeClick(object sender, RoutedEventArgs e)
    {
        CloseHeaderMenu();
        _viewModel.TogglePrivacyMode();
    }

    /// <summary>
    /// Decreases the scale used by the calendar body.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnDecreaseCalendarZoomClick(object sender, RoutedEventArgs e)
    {
        ChangeCalendarZoom(CalendarZoomKeyboardShortcutAction.Decrease);
    }

    /// <summary>
    /// Increases the scale used by the calendar body.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnIncreaseCalendarZoomClick(object sender, RoutedEventArgs e)
    {
        ChangeCalendarZoom(CalendarZoomKeyboardShortcutAction.Increase);
    }

    /// <summary>
    /// Restores the calendar body scale to the default value.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The routed event arguments.</param>
    private void OnResetCalendarZoomClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetCalendarZoom();
        ApplyCalendarZoomLayoutUpdate();
    }

    /// <summary>
    /// Recalculates calendar layout after fine-grained zoom changes.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The changed value arguments.</param>
    private void OnCalendarZoomValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DataContext is not MainWindowViewModel)
        {
            return;
        }

        _viewModel.SetCalendarZoomScale(e.NewValue);
        ApplyCalendarZoomLayoutUpdate();
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

    private void ApplyCalendarZoomLayoutUpdate()
    {
        UpdateLayout();
        UpdateScheduleWidth();
    }

    private void ChangeCalendarZoom(CalendarZoomKeyboardShortcutAction action)
    {
        var changed = action switch
        {
            CalendarZoomKeyboardShortcutAction.Decrease => _viewModel.DecreaseCalendarZoom(),
            CalendarZoomKeyboardShortcutAction.Increase => _viewModel.IncreaseCalendarZoom(),
            _ => false
        };

        if (changed)
        {
            ApplyCalendarZoomLayoutUpdate();
        }
    }

    private static Key GetShortcutKey(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.System)
        {
            return e.SystemKey;
        }

        return e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key;
    }

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
    /// Refreshes date-sensitive state when the window becomes active after being idle.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private async void OnActivatedAsync(object? sender, EventArgs e) => await RefreshCurrentDateAfterIdleAsync();

    /// <summary>
    /// Applies post-show layout updates after the tray restores the main window.
    /// </summary>
    private void OnShownFromTray()
    {
        UpdateLayout();
        UpdateScheduleWidth();
        ScrollScheduleToNowLine();
        _ = RefreshCurrentDateAfterIdleAsync();
    }

    /// <summary>
    /// Releases window-scoped controllers and the bound view model when the window closes.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnClosed(object? sender, EventArgs e)
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChangedAsync;
        SystemEvents.SessionSwitch -= OnSessionSwitchAsync;
        Activated -= OnActivatedAsync;
        _themeManager.ThemeChanged -= OnThemeChanged;
        _themeController.Detach(this);
        _viewModel.Dispose();
    }

    private async void OnPowerModeChangedAsync(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            await RefreshCurrentDateAfterIdleAsync();
        }
    }

    private async void OnSessionSwitchAsync(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            await RefreshCurrentDateAfterIdleAsync();
        }
    }

    private async Task RefreshCurrentDateAfterIdleAsync()
    {
        if (!Dispatcher.CheckAccess())
        {
            await await Dispatcher.InvokeAsync(RefreshCurrentDateAfterIdleAsync);
            return;
        }

        if (await _viewModel.RefreshCurrentDateIfChangedAsync())
        {
            ScrollScheduleToNowLine();
        }
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
