namespace DayScope.Platform;

/// <summary>
/// Opens folders through the operating-system shell.
/// </summary>
public interface IFolderLauncher
{
    /// <summary>
    /// Attempts to open the provided folder path.
    /// </summary>
    /// <param name="folderPath">The folder path to open.</param>
    void Open(string folderPath);
}
