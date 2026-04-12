namespace DayScope.Platform;

/// <summary>
/// Writes text to the system clipboard.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Attempts to set clipboard text.
    /// </summary>
    /// <param name="text">The text to copy.</param>
    /// <returns><see langword="true"/> when the text was copied; otherwise <see langword="false"/>.</returns>
    bool TrySetText(string text);
}
