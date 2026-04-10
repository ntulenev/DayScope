using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.ComponentModel;

using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;
using DayScope.ViewModels;

namespace DayScope.Views;

public partial class MainWindow : Window
{
    public MainWindow(
        MainWindowViewModel viewModel,
        IOptions<WindowSettings> windowOptions)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(windowOptions);

        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        ApplyWindowSettings(windowOptions.Value);
        SourceInitialized += (_, _) => ApplyDarkTitleBar();
        SizeChanged += (_, _) => UpdateScheduleWidth();
        Closing += OnClosing;
        Closed += (_, _) => _viewModel.Dispose();
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

    private void ApplyDarkTitleBar()
    {
        var windowHandle = new WindowInteropHelper(this).Handle;
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        var enabled = 1;
        _ = DwmSetWindowAttribute(
            windowHandle,
            DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref enabled,
            sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private bool _allowClose;
    private readonly MainWindowViewModel _viewModel;
}
