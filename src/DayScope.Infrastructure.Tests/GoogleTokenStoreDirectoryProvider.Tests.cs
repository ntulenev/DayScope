using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Configuration;
using DayScope.Infrastructure.Google;

namespace DayScope.Infrastructure.Tests;

public sealed class GoogleTokenStoreDirectoryProviderTests
{
    [Fact(DisplayName = "The token-store provider uses the resolved configured path when one is available.")]
    [Trait("Category", "Unit")]
    public void GetTokenStoreDirectoryShouldUseResolvedConfiguredPathWhenAvailable()
    {
        // Arrange
        var configuredPath = Path.Combine(Path.GetTempPath(), "DayScope.Tests", Guid.NewGuid().ToString("N"));
        var resolvePathCalls = 0;
        var pathResolver = new Mock<IPathResolver>(MockBehavior.Strict);
        pathResolver.Setup(resolver => resolver.ResolvePath("configured"))
            .Callback(() => resolvePathCalls++)
            .Returns(configuredPath);
        var provider = new GoogleTokenStoreDirectoryProvider(
            Options.Create(new GoogleCalendarSettings { TokenStoreDirectory = "configured" }),
            pathResolver.Object);

        try
        {
            // Act
            var tokenStoreDirectory = provider.GetTokenStoreDirectory();

            // Assert
            tokenStoreDirectory.Should().Be(configuredPath);
            Directory.Exists(configuredPath).Should().BeTrue();
            resolvePathCalls.Should().Be(1);
        }
        finally
        {
            if (Directory.Exists(configuredPath))
            {
                Directory.Delete(configuredPath, recursive: true);
            }
        }
    }

    [Fact(DisplayName = "The token-store provider falls back to the local application-data directory when the configured path is blank.")]
    [Trait("Category", "Unit")]
    public void GetTokenStoreDirectoryShouldFallbackToLocalApplicationDataWhenConfiguredPathIsBlank()
    {
        // Arrange
        var expectedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DayScope",
            "GoogleCalendarToken");
        var resolvePathCalls = 0;
        var pathResolver = new Mock<IPathResolver>(MockBehavior.Strict);
        pathResolver.Setup(resolver => resolver.ResolvePath(string.Empty))
            .Callback(() => resolvePathCalls++)
            .Returns(string.Empty);
        var provider = new GoogleTokenStoreDirectoryProvider(
            Options.Create(new GoogleCalendarSettings { TokenStoreDirectory = string.Empty }),
            pathResolver.Object);

        // Act
        var tokenStoreDirectory = provider.GetTokenStoreDirectory();

        // Assert
        tokenStoreDirectory.Should().Be(expectedPath);
        Directory.Exists(expectedPath).Should().BeTrue();
        resolvePathCalls.Should().Be(1);
    }
}
