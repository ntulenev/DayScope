using System.Runtime.InteropServices;

namespace DayScope.Platform;

/// <summary>
/// Clipboard implementation backed by WPF clipboard APIs.
/// </summary>
public sealed class WpfClipboardService : IClipboardService
{
    /// <inheritdoc />
    public bool TrySetText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        try
        {
            System.Windows.Clipboard.SetText(text);
            return true;
        }
        catch (COMException)
        {
            return false;
        }
        catch (ExternalException)
        {
            return false;
        }
    }
}
