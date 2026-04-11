namespace DayScope.Infrastructure.Configuration;

/// <summary>
/// Resolves configured file-system paths relative to known application locations.
/// </summary>
internal static class PathResolver
{
    /// <summary>
    /// Resolves a configured path into an absolute file-system path when possible.
    /// </summary>
    /// <param name="configuredPath">The configured path value.</param>
    /// <returns>The resolved absolute path, or an empty string when the value is blank.</returns>
    internal static string ResolvePath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        var expandedPath = Environment.ExpandEnvironmentVariables(configuredPath.Trim());
        if (Path.IsPathRooted(expandedPath))
        {
            return expandedPath;
        }

        var currentDirectoryPath = Path.GetFullPath(expandedPath, Environment.CurrentDirectory);
        if (File.Exists(currentDirectoryPath) || Directory.Exists(currentDirectoryPath))
        {
            return currentDirectoryPath;
        }

        return Path.GetFullPath(expandedPath, AppContext.BaseDirectory);
    }
}
