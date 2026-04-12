using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3.Data;

namespace DayScope.Infrastructure.Calendar;

/// <summary>
/// Executes Google Calendar API requests for a bounded time range.
/// </summary>
public interface IGoogleCalendarGateway
{
    /// <summary>
    /// Loads raw Google Calendar events for the requested range.
    /// </summary>
    /// <param name="credential">The authorized Google credential.</param>
    /// <param name="calendarId">The target Google Calendar identifier.</param>
    /// <param name="startOfDay">The inclusive start instant.</param>
    /// <param name="endOfDay">The exclusive end instant.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>The raw Google Calendar events.</returns>
    Task<IReadOnlyList<Event>> GetEventsAsync(
        UserCredential credential,
        string calendarId,
        DateTimeOffset startOfDay,
        DateTimeOffset endOfDay,
        CancellationToken cancellationToken);
}
