namespace DayScope.Infrastructure.Configuration;

/// <summary>
/// Resolves configured paths into absolute file-system paths.
/// </summary>
public interface IPathResolver
{
    /// <summary>
    /// Resolves a configured path into an absolute file-system path when possible.
    /// </summary>
    /// <param name="configuredPath">The configured path value.</param>
    /// <returns>The resolved absolute path, or an empty string when the value is blank.</returns>
    string ResolvePath(string? configuredPath);
}
