using FluentAssertions;

using Google;
using Google.Apis.Auth.OAuth2.Responses;

using DayScope.Application.Calendar;
using DayScope.Infrastructure.Calendar;

namespace DayScope.Infrastructure.Tests;

public sealed class GoogleCalendarFailureMapperTests
{
    [Fact(DisplayName = "The mapper throws when the exception is null.")]
    [Trait("Category", "Unit")]
    public void MapShouldThrowWhenExceptionIsNull()
    {
        // Arrange
        var mapper = new GoogleCalendarFailureMapper();

        // Act
        var action = () => mapper.Map(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Token response failures map to authorization required.")]
    [Trait("Category", "Unit")]
    public void MapShouldReturnAuthorizationRequiredWhenExceptionIsTokenResponseException()
    {
        // Arrange
        var mapper = new GoogleCalendarFailureMapper();

        // Act
        var status = mapper.Map(new TokenResponseException(new TokenErrorResponse()));

        // Assert
        status.Should().Be(CalendarLoadStatus.AuthorizationRequired);
    }

    [Fact(DisplayName = "Google API failures map to access denied.")]
    [Trait("Category", "Unit")]
    public void MapShouldReturnAccessDeniedWhenExceptionIsGoogleApiException()
    {
        // Arrange
        var mapper = new GoogleCalendarFailureMapper();

        // Act
        var status = mapper.Map(new GoogleApiException("Calendar", "Denied"));

        // Assert
        status.Should().Be(CalendarLoadStatus.AccessDenied);
    }

    [Fact(DisplayName = "Task cancellations map to authorization required.")]
    [Trait("Category", "Unit")]
    public void MapShouldReturnAuthorizationRequiredWhenExceptionIsTaskCanceledException()
    {
        // Arrange
        var mapper = new GoogleCalendarFailureMapper();

        // Act
        var status = mapper.Map(new TaskCanceledException());

        // Assert
        status.Should().Be(CalendarLoadStatus.AuthorizationRequired);
    }

    [Fact(DisplayName = "Unknown failures map to unavailable.")]
    [Trait("Category", "Unit")]
    public void MapShouldReturnUnavailableWhenExceptionIsUnknown()
    {
        // Arrange
        var mapper = new GoogleCalendarFailureMapper();

        // Act
        var status = mapper.Map(new InvalidOperationException("Boom"));

        // Assert
        status.Should().Be(CalendarLoadStatus.Unavailable);
    }
}
