using System.ComponentModel;
using System.Diagnostics;

namespace DayScope.Platform;

/// <summary>
/// Opens URIs through the Windows shell.
/// </summary>
public sealed class ShellUriLauncher : IUriLauncher
{
    /// <inheritdoc />
    public void Open(Uri uri)
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
        }
        catch (Win32Exception)
        {
        }
    }
}
