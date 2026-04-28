using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace DayScope.Platform;

/// <summary>
/// Opens folders through the Windows shell.
/// </summary>
public sealed class ShellFolderLauncher : IFolderLauncher
{
    /// <inheritdoc />
    public void Open(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(folderPath);
            Process.Start(new ProcessStartInfo(folderPath)
            {
                UseShellExecute = true
            });
        }
        catch (ArgumentException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (Win32Exception)
        {
        }
    }
}
