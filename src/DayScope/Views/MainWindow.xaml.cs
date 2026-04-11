using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;

using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;
using DayScope.Themes;
using DayScope.ViewModels;

namespace DayScope.Views;

public partial class MainWindow : Window
{
    public MainWindow(
        MainWindowViewModel viewModel,
        ThemeManager themeManager,
        IOptions<WindowSettings> windowOptions)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(themeManager);
        ArgumentNullException.ThrowIfNull(windowOptions);

        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _themeManager = themeManager;
        ApplyWindowSettings(windowOptions.Value);
        SourceInitialized += (_, _) => ApplyWindowTitleBarTheme();
        SizeChanged += (_, _) => UpdateScheduleWidth();
        Closing += OnClosing;
        Closed += (_, _) =>
        {
            _themeManager.ThemeChanged -= OnThemeChanged;
            _viewModel.Dispose();
        };
        _themeManager.ThemeChanged += OnThemeChanged;
    }

    public async Task InitializeAsync()
    {
        UpdateScheduleWidth();
        await _viewModel.InitializeAsync();
    }

    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();

        UpdateLayout();
        UpdateScheduleWidth();
        ScrollScheduleToNowLine();
    }

    public void HideToTray()
    {
        Hide();
    }

    public Task RefreshNowAsync() => _viewModel.RefreshNowAsync();

    public void CloseFromTray()
    {
        _allowClose = true;
        Close();
    }

    private void OnEventCardMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        _viewModel.OpenEventDetails(element.DataContext);
        e.Handled = true;
    }

    private void OnEventDetailsOverlayMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _viewModel.CloseEventDetails();
        e.Handled = true;
    }

    private void OnCloseEventDetailsClick(object sender, RoutedEventArgs e)
    {
        _viewModel.CloseEventDetails();
    }

    private void OnOpenEventLinkClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedEventDetails?.JoinUrl is not Uri joinUrl)
        {
            return;
        }

        OpenUri(joinUrl);
    }

    private void OnOpenUnreadEmailsClick(object sender, RoutedEventArgs e)
    {
        OpenUri(_viewModel.UnreadEmailInboxUri);
    }

    private void OnOpenGoogleCalendarClick(object sender, RoutedEventArgs e)
    {
        OpenUri(_viewModel.GoogleCalendarUri);
    }

    private async void OnPreviousDayClickAsync(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateDaysAsync(-1);
        ScrollScheduleToNowLine();
    }

    private async void OnNextDayClickAsync(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateDaysAsync(1);
        ScrollScheduleToNowLine();
    }

    private void OnCopyEventLinkClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedEventDetails?.JoinUrl is not Uri joinUrl)
        {
            return;
        }

        try
        {
            System.Windows.Clipboard.SetText(joinUrl.AbsoluteUri);
        }
        catch (COMException)
        {
            // Ignore clipboard access failures and keep the dialog open.
        }
        catch (ExternalException)
        {
            // Ignore clipboard access failures and keep the dialog open.
        }
    }

    private void ApplyWindowSettings(WindowSettings settings)
    {
        Width = settings.Width;
        Height = settings.Height;
        MinWidth = settings.MinWidth;
        MinHeight = settings.MinHeight;
    }

    private void UpdateScheduleWidth()
    {
        var availableWidth = ScheduleSurfaceBorder.ActualWidth > 0
            ? ScheduleSurfaceBorder.ActualWidth - 18
            : ActualWidth - 280;

        _viewModel.UpdateAvailableScheduleWidth(availableWidth);
    }

    private void ScrollScheduleToNowLine()
    {
        var targetOffset = Math.Max(0, _viewModel.NowLineTop - 280);
        ScheduleScrollViewer.ScrollToVerticalOffset(targetOffset);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        HideToTray();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(ApplyWindowTitleBarTheme);
    }

    private void ApplyWindowTitleBarTheme()
    {
        var windowHandle = new WindowInteropHelper(this).Handle;
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        var enabled = _themeManager.IsDarkTheme ? 1 : 0;
        _ = DwmSetWindowAttribute(
            windowHandle,
            DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref enabled,
            sizeof(int));
    }

    private static void OpenUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri)
            {
                UseShellExecute = true
            });
        }
        catch (InvalidOperationException)
        {
            // Ignore shell launch failures.
        }
        catch (Win32Exception)
        {
            // Ignore shell launch failures.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private bool _allowClose;
    private readonly ThemeManager _themeManager;
    private readonly MainWindowViewModel _viewModel;
}
