using Google;
using Google.Apis.Auth.OAuth2.Responses;

using DayScope.Application.Calendar;

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
            GoogleApiException => CalendarLoadStatus.AccessDenied,
            TaskCanceledException => CalendarLoadStatus.AuthorizationRequired,
            _ => CalendarLoadStatus.Unavailable
        };
    }
}
