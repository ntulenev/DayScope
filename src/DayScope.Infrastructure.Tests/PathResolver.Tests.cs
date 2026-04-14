using FluentAssertions;

using DayScope.Infrastructure.Configuration;

namespace DayScope.Infrastructure.Tests;

public sealed class PathResolverTests
{
    [Fact(DisplayName = "Blank configured paths resolve to an empty string.")]
    [Trait("Category", "Unit")]
    public void ResolvePathShouldReturnEmptyStringWhenConfiguredPathIsBlank()
    {
        // Arrange
        var resolver = new PathResolver();

        // Act
        var resolvedPath = resolver.ResolvePath("   ");

        // Assert
        resolvedPath.Should().BeEmpty();
    }

    [Fact(DisplayName = "Rooted configured paths expand environment variables and remain absolute.")]
    [Trait("Category", "Unit")]
    public void ResolvePathShouldExpandEnvironmentVariablesForRootedPaths()
    {
        // Arrange
        var resolver = new PathResolver();
        var configuredPath = @"%TEMP%\dayscope\config.json";

        // Act
        var resolvedPath = resolver.ResolvePath(configuredPath);

        // Assert
        resolvedPath.Should().Be(Environment.ExpandEnvironmentVariables(configuredPath));
    }

    [Fact(DisplayName = "Relative paths prefer the current directory when the file exists there.")]
    [Trait("Category", "Unit")]
    public void ResolvePathShouldPreferCurrentDirectoryWhenRelativePathExistsThere()
    {
        // Arrange
        var resolver = new PathResolver();
        var relativePath = Path.Combine("TestArtifacts", $"{Guid.NewGuid():N}.txt");
        var fullPath = Path.GetFullPath(relativePath, Environment.CurrentDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "test");

        try
        {
            // Act
            var resolvedPath = resolver.ResolvePath(relativePath);

            // Assert
            resolvedPath.Should().Be(fullPath);
        }
        finally
        {
            var directoryPath = Path.GetDirectoryName(fullPath)!;
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    [Fact(DisplayName = "Relative paths fall back to the application base directory when they do not exist in the current directory.")]
    [Trait("Category", "Unit")]
    public void ResolvePathShouldFallbackToApplicationBaseDirectoryWhenRelativePathDoesNotExistInCurrentDirectory()
    {
        // Arrange
        var resolver = new PathResolver();
        var relativePath = Path.Combine("missing", $"{Guid.NewGuid():N}.json");

        // Act
        var resolvedPath = resolver.ResolvePath(relativePath);

        // Assert
        resolvedPath.Should().Be(Path.GetFullPath(relativePath, AppContext.BaseDirectory));
    }
}
