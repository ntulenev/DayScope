using System.Runtime.CompilerServices;

using FluentAssertions;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3.Data;

using Microsoft.Extensions.Options;

using Moq;

using DayScope.Application.Calendar;
using DayScope.Domain.Calendar;
using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Calendar;
using DayScope.Infrastructure.Google;

namespace DayScope.Infrastructure.Tests;

public sealed class GoogleCalendarServiceTests
{
    [Fact(DisplayName = "The constructor throws when the settings are null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenSettingsAreNull()
    {
        // Arrange
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict).Object;
        var calendarGateway = new Mock<IGoogleCalendarGateway>(MockBehavior.Strict).Object;
        var calendarEventMapper = new Mock<IGoogleCalendarEventMapper>(MockBehavior.Strict).Object;
        var calendarFailureMapper = new Mock<IGoogleCalendarFailureMapper>(MockBehavior.Strict).Object;

        // Act
        var action = () => new GoogleCalendarService(
            null!,
            credentialProvider,
            calendarGateway,
            calendarEventMapper,
            calendarFailureMapper);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The constructor throws when the credential provider is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenCredentialProviderIsNull()
    {
        // Arrange
        var calendarGateway = new Mock<IGoogleCalendarGateway>(MockBehavior.Strict).Object;
        var calendarEventMapper = new Mock<IGoogleCalendarEventMapper>(MockBehavior.Strict).Object;
        var calendarFailureMapper = new Mock<IGoogleCalendarFailureMapper>(MockBehavior.Strict).Object;

        // Act
        var action = () => new GoogleCalendarService(
            Options.Create(new GoogleCalendarSettings()),
            null!,
            calendarGateway,
            calendarEventMapper,
            calendarFailureMapper);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The constructor throws when the calendar gateway is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenCalendarGatewayIsNull()
    {
        // Arrange
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict).Object;
        var calendarEventMapper = new Mock<IGoogleCalendarEventMapper>(MockBehavior.Strict).Object;
        var calendarFailureMapper = new Mock<IGoogleCalendarFailureMapper>(MockBehavior.Strict).Object;

        // Act
        var action = () => new GoogleCalendarService(
            Options.Create(new GoogleCalendarSettings()),
            credentialProvider,
            null!,
            calendarEventMapper,
            calendarFailureMapper);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The constructor throws when the calendar event mapper is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenCalendarEventMapperIsNull()
    {
        // Arrange
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict).Object;
        var calendarGateway = new Mock<IGoogleCalendarGateway>(MockBehavior.Strict).Object;
        var calendarFailureMapper = new Mock<IGoogleCalendarFailureMapper>(MockBehavior.Strict).Object;

        // Act
        var action = () => new GoogleCalendarService(
            Options.Create(new GoogleCalendarSettings()),
            credentialProvider,
            calendarGateway,
            null!,
            calendarFailureMapper);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The constructor throws when the calendar failure mapper is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenCalendarFailureMapperIsNull()
    {
        // Arrange
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict).Object;
        var calendarGateway = new Mock<IGoogleCalendarGateway>(MockBehavior.Strict).Object;
        var calendarEventMapper = new Mock<IGoogleCalendarEventMapper>(MockBehavior.Strict).Object;

        // Act
        var action = () => new GoogleCalendarService(
            Options.Create(new GoogleCalendarSettings()),
            credentialProvider,
            calendarGateway,
            calendarEventMapper,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The enabled state mirrors the credential provider.")]
    [Trait("Category", "Unit")]
    public void IsEnabledShouldMirrorCredentialProvider()
    {
        // Arrange
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict);
        credentialProvider.SetupGet(provider => provider.IsEnabled)
            .Returns(true);
        var service = CreateService(credentialProvider: credentialProvider.Object);

        // Act
        var isEnabled = service.IsEnabled;

        // Assert
        isEnabled.Should().BeTrue();
    }

    [Fact(DisplayName = "Loading events throws when the time zone is null.")]
    [Trait("Category", "Unit")]
    public async Task GetEventsForDateAsyncShouldThrowWhenTimeZoneIsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        Func<Task> action = () => service.GetEventsForDateAsync(
            new DateOnly(2026, 4, 14),
            null!,
            CalendarInteractionMode.Interactive,
            CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(DisplayName = "Loading events returns disabled when Google integration is disabled.")]
    [Trait("Category", "Unit")]
    public async Task GetEventsForDateAsyncShouldReturnDisabledWhenServiceIsDisabled()
    {
        // Arrange
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict);
        credentialProvider.SetupGet(provider => provider.IsEnabled)
            .Returns(false);
        var service = CreateService(credentialProvider: credentialProvider.Object);

        // Act
        var result = await service.GetEventsForDateAsync(
            new DateOnly(2026, 4, 14),
            TimeZoneInfo.Utc,
            CalendarInteractionMode.Interactive,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(CalendarLoadStatus.Disabled);
        result.Agenda.Events.Should().BeEmpty();
    }

    [Fact(DisplayName = "Loading events returns disabled when the credential provider reports disabled.")]
    [Trait("Category", "Unit")]
    public async Task GetEventsForDateAsyncShouldReturnDisabledWhenCredentialProviderReturnsDisabled()
    {
        // Arrange
        var credentialRequests = 0;
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict);
        credentialProvider.SetupGet(provider => provider.IsEnabled)
            .Returns(true);
        credentialProvider.Setup(provider => provider.GetCredentialAsync(
                true,
                CancellationToken.None))
            .Callback(() => credentialRequests++)
            .ReturnsAsync(GoogleCredentialLoadResult.Disabled());
        var service = CreateService(credentialProvider: credentialProvider.Object);

        // Act
        var result = await service.GetEventsForDateAsync(
            new DateOnly(2026, 4, 14),
            TimeZoneInfo.Utc,
            CalendarInteractionMode.Interactive,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(CalendarLoadStatus.Disabled);
        credentialRequests.Should().Be(1);
    }

    [Fact(DisplayName = "Loading events returns client-secrets-missing when the credential provider reports that state.")]
    [Trait("Category", "Unit")]
    public async Task GetEventsForDateAsyncShouldReturnClientSecretsMissingWhenCredentialProviderReturnsClientSecretsMissing()
    {
        // Arrange
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict);
        credentialProvider.SetupGet(provider => provider.IsEnabled)
            .Returns(true);
        credentialProvider.Setup(provider => provider.GetCredentialAsync(
                false,
                CancellationToken.None))
            .ReturnsAsync(GoogleCredentialLoadResult.ClientSecretsMissing());
        var service = CreateService(credentialProvider: credentialProvider.Object);

        // Act
        var result = await service.GetEventsForDateAsync(
            new DateOnly(2026, 4, 14),
            TimeZoneInfo.Utc,
            CalendarInteractionMode.Background,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(CalendarLoadStatus.ClientSecretsMissing);
    }

    [Fact(DisplayName = "Loading events returns authorization required when the credential provider does not supply a credential.")]
    [Trait("Category", "Unit")]
    public async Task GetEventsForDateAsyncShouldReturnAuthorizationRequiredWhenCredentialIsMissing()
    {
        // Arrange
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict);
        credentialProvider.SetupGet(provider => provider.IsEnabled)
            .Returns(true);
        credentialProvider.Setup(provider => provider.GetCredentialAsync(
                true,
                CancellationToken.None))
            .ReturnsAsync(GoogleCredentialLoadResult.AuthorizationRequired());
        var service = CreateService(credentialProvider: credentialProvider.Object);

        // Act
        var result = await service.GetEventsForDateAsync(
            new DateOnly(2026, 4, 14),
            TimeZoneInfo.Utc,
            CalendarInteractionMode.Interactive,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(CalendarLoadStatus.AuthorizationRequired);
    }

    [Fact(DisplayName = "Loading events returns no-events when the mapped agenda is empty after filtering.")]
    [Trait("Category", "Unit")]
    public async Task GetEventsForDateAsyncShouldReturnNoEventsWhenMappedAgendaIsEmpty()
    {
        // Arrange
        var day = new DateOnly(2026, 4, 14);
        var timeZone = TimeZoneInfo.Utc;
        var credential = CreateCredential();
        var sourceEvent = new Event { Summary = "Ignored" };
        var mappedEvents = 0;
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict);
        credentialProvider.SetupGet(provider => provider.IsEnabled)
            .Returns(true);
        credentialProvider.Setup(provider => provider.GetCredentialAsync(
                true,
                CancellationToken.None))
            .ReturnsAsync(GoogleCredentialLoadResult.Success(credential));
        var calendarGateway = new Mock<IGoogleCalendarGateway>(MockBehavior.Strict);
        calendarGateway.Setup(gateway => gateway.GetEventsAsync(
                credential,
                "primary",
                new DateTimeOffset(2026, 4, 14, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 15, 0, 0, 0, TimeSpan.Zero),
                CancellationToken.None))
            .ReturnsAsync([sourceEvent]);
        var calendarEventMapper = new Mock<IGoogleCalendarEventMapper>(MockBehavior.Strict);
        calendarEventMapper.Setup(mapper => mapper.MapEvent(sourceEvent, timeZone))
            .Callback(() => mappedEvents++)
            .Returns((CalendarEvent?)null);
        var service = CreateService(
            credentialProvider.Object,
            calendarGateway.Object,
            calendarEventMapper.Object);

        // Act
        var result = await service.GetEventsForDateAsync(
            day,
            timeZone,
            CalendarInteractionMode.Interactive,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(CalendarLoadStatus.NoEvents);
        result.Agenda.Events.Should().BeEmpty();
        mappedEvents.Should().Be(1);
    }

    [Fact(DisplayName = "Loading events returns the intersecting mapped events in chronological order.")]
    [Trait("Category", "Unit")]
    public async Task GetEventsForDateAsyncShouldReturnIntersectingMappedEventsInChronologicalOrder()
    {
        // Arrange
        var day = new DateOnly(2026, 4, 14);
        var timeZone = TimeZoneInfo.Utc;
        var credential = CreateCredential();
        var firstSourceEvent = new Event { Summary = "Later" };
        var secondSourceEvent = new Event { Summary = "Earlier" };
        var laterEvent = CreateCalendarEvent(
            "Later",
            new DateTimeOffset(2026, 4, 14, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 12, 0, 0, TimeSpan.Zero));
        var earlierEvent = CreateCalendarEvent(
            "Earlier",
            new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 10, 0, 0, TimeSpan.Zero));
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict);
        credentialProvider.SetupGet(provider => provider.IsEnabled)
            .Returns(true);
        credentialProvider.Setup(provider => provider.GetCredentialAsync(
                false,
                CancellationToken.None))
            .ReturnsAsync(GoogleCredentialLoadResult.Success(credential));
        var calendarGateway = new Mock<IGoogleCalendarGateway>(MockBehavior.Strict);
        calendarGateway.Setup(gateway => gateway.GetEventsAsync(
                credential,
                "work",
                new DateTimeOffset(2026, 4, 14, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 15, 0, 0, 0, TimeSpan.Zero),
                CancellationToken.None))
            .ReturnsAsync([firstSourceEvent, secondSourceEvent]);
        var calendarEventMapper = new Mock<IGoogleCalendarEventMapper>(MockBehavior.Strict);
        calendarEventMapper.Setup(mapper => mapper.MapEvent(firstSourceEvent, timeZone))
            .Returns(laterEvent);
        calendarEventMapper.Setup(mapper => mapper.MapEvent(secondSourceEvent, timeZone))
            .Returns(earlierEvent);
        var service = CreateService(
            credentialProvider.Object,
            calendarGateway.Object,
            calendarEventMapper.Object,
            settings: new GoogleCalendarSettings { CalendarId = "work" });

        // Act
        var result = await service.GetEventsForDateAsync(
            day,
            timeZone,
            CalendarInteractionMode.Background,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(CalendarLoadStatus.Success);
        result.Agenda.Events.Should().ContainInOrder(earlierEvent, laterEvent);
    }

    [Fact(DisplayName = "Loading events maps infrastructure failures through the failure mapper.")]
    [Trait("Category", "Unit")]
    public async Task GetEventsForDateAsyncShouldMapInfrastructureFailuresThroughFailureMapper()
    {
        // Arrange
        var credential = CreateCredential();
        var day = new DateOnly(2026, 4, 14);
        var failure = new InvalidOperationException("Boom");
        Exception? mappedException = null;
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict);
        credentialProvider.SetupGet(provider => provider.IsEnabled)
            .Returns(true);
        credentialProvider.Setup(provider => provider.GetCredentialAsync(
                true,
                CancellationToken.None))
            .ReturnsAsync(GoogleCredentialLoadResult.Success(credential));
        var calendarGateway = new Mock<IGoogleCalendarGateway>(MockBehavior.Strict);
        calendarGateway.Setup(gateway => gateway.GetEventsAsync(
                credential,
                "primary",
                new DateTimeOffset(2026, 4, 14, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 15, 0, 0, 0, TimeSpan.Zero),
                CancellationToken.None))
            .ThrowsAsync(failure);
        var calendarFailureMapper = new Mock<IGoogleCalendarFailureMapper>(MockBehavior.Strict);
        calendarFailureMapper.Setup(mapper => mapper.Map(failure))
            .Callback<Exception>(exception => mappedException = exception)
            .Returns(CalendarLoadStatus.Unavailable);
        var service = CreateService(
            credentialProvider.Object,
            calendarGateway.Object,
            calendarFailureMapper: calendarFailureMapper.Object);

        // Act
        var result = await service.GetEventsForDateAsync(
            day,
            TimeZoneInfo.Utc,
            CalendarInteractionMode.Interactive,
            CancellationToken.None);

        // Assert
        result.Status.Should().Be(CalendarLoadStatus.Unavailable);
        mappedException.Should().BeSameAs(failure);
    }

    private static GoogleCalendarService CreateService(
        IGoogleCredentialProvider? credentialProvider = null,
        IGoogleCalendarGateway? calendarGateway = null,
        IGoogleCalendarEventMapper? calendarEventMapper = null,
        IGoogleCalendarFailureMapper? calendarFailureMapper = null,
        GoogleCalendarSettings? settings = null)
    {
        return new GoogleCalendarService(
            Options.Create(settings ?? new GoogleCalendarSettings()),
            credentialProvider ?? CreateDefaultCredentialProvider(),
            calendarGateway ?? CreateDefaultCalendarGateway(),
            calendarEventMapper ?? CreateDefaultCalendarEventMapper(),
            calendarFailureMapper ?? CreateDefaultCalendarFailureMapper());
    }

    private static IGoogleCredentialProvider CreateDefaultCredentialProvider()
    {
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict);
        credentialProvider.SetupGet(provider => provider.IsEnabled)
            .Returns(true);
        credentialProvider.Setup(provider => provider.GetCredentialAsync(
                true,
                CancellationToken.None))
            .ReturnsAsync(GoogleCredentialLoadResult.AuthorizationRequired());
        credentialProvider.Setup(provider => provider.GetCredentialAsync(
                false,
                CancellationToken.None))
            .ReturnsAsync(GoogleCredentialLoadResult.AuthorizationRequired());

        return credentialProvider.Object;
    }

    private static IGoogleCalendarGateway CreateDefaultCalendarGateway()
        => new Mock<IGoogleCalendarGateway>(MockBehavior.Strict).Object;

    private static IGoogleCalendarEventMapper CreateDefaultCalendarEventMapper()
        => new Mock<IGoogleCalendarEventMapper>(MockBehavior.Strict).Object;

    private static IGoogleCalendarFailureMapper CreateDefaultCalendarFailureMapper()
        => new Mock<IGoogleCalendarFailureMapper>(MockBehavior.Strict).Object;

    private static UserCredential CreateCredential()
        => (UserCredential)RuntimeHelpers.GetUninitializedObject(typeof(UserCredential));

    private static CalendarEvent CreateCalendarEvent(
        string title,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        return new CalendarEvent(
            title,
            start,
            end,
            false,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            null,
            null,
            null,
            null,
            []);
    }
}
