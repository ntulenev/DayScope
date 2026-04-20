using Google;
using Google.Apis.Auth.OAuth2.Responses;

using DayScope.Application.Calendar;
using DayScope.Infrastructure.Google;

namespace DayScope.Infrastructure.Calendar;

/// <summary>
/// Maps Google Calendar exceptions to DayScope calendar load statuses.
/// </summary>
public sealed class GoogleCalendarFailureMapper : IGoogleCalendarFailureMapper
{
    /// <inheritdoc />
    public CalendarLoadStatus Map(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            TokenResponseException => CalendarLoadStatus.AuthorizationRequired,
            _ when GoogleConnectivityFailureDetector.IsConnectivityFailure(exception) =>
                CalendarLoadStatus.Unavailable,
            GoogleApiException => CalendarLoadStatus.AccessDenied,
            _ => CalendarLoadStatus.Unavailable
        };
    }
}
