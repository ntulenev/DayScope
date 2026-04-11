namespace DayScope.Domain.Calendar;

/// <summary>
/// Represents the signed-in user's participation state for an event.
/// </summary>
public enum CalendarParticipationStatus
{
    Accepted = 0,
    AwaitingResponse = 1,
    Tentative = 2,
    Declined = 3,
    Cancelled = 4
}

/// <summary>
/// Represents the source event kind returned by Google Calendar.
/// </summary>
public enum CalendarEventKind
{
    Default = 0,
    FocusTime = 1,
    OutOfOffice = 2,
    WorkingLocation = 3,
    Task = 4,
    AppointmentSchedule = 5
}

/// <summary>
/// Represents a participant associated with a calendar event.
/// </summary>
public sealed record CalendarEventParticipant
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarEventParticipant"/> class.
    /// </summary>
    /// <param name="displayName">The participant display name.</param>
    /// <param name="email">The participant email address.</param>
    /// <param name="participationStatus">The participant response status.</param>
    /// <param name="isSelf">Whether the participant represents the signed-in user.</param>
    public CalendarEventParticipant(
        string? displayName,
        string? email,
        CalendarParticipationStatus participationStatus,
        bool isSelf)
    {
        DisplayName = NormalizeOptionalText(displayName);
        Email = NormalizeOptionalText(email);
        ParticipationStatus = participationStatus;
        IsSelf = isSelf;
    }

    public string? DisplayName { get; }

    public string? Email { get; }

    public CalendarParticipationStatus ParticipationStatus { get; }

    public bool IsSelf { get; }

    public string DisplayLabel =>
        !string.IsNullOrWhiteSpace(DisplayName)
            ? DisplayName.Trim()
            : !string.IsNullOrWhiteSpace(Email)
                ? Email.Trim()
                : "Unknown participant";

    /// <summary>
    /// Normalizes optional participant text fields into trimmed nullable values.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <returns>The trimmed value, or <see langword="null"/> when blank.</returns>
    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}

/// <summary>
/// Represents a normalized calendar event used by the application.
/// </summary>
public sealed record CalendarEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarEvent"/> class.
    /// </summary>
    /// <param name="title">The event title.</param>
    /// <param name="start">The event start time.</param>
    /// <param name="end">The event end time, if any.</param>
    /// <param name="isAllDay">Whether the event spans the entire day.</param>
    /// <param name="participationStatus">The signed-in user's participation status.</param>
    /// <param name="eventKind">The event kind.</param>
    /// <param name="organizerName">The organizer display name.</param>
    /// <param name="organizerEmail">The organizer email address.</param>
    /// <param name="description">The event description.</param>
    /// <param name="joinUrl">The meeting link.</param>
    /// <param name="participants">The participants associated with the event.</param>
    public CalendarEvent(
        string title,
        DateTimeOffset start,
        DateTimeOffset? end,
        bool isAllDay,
        CalendarParticipationStatus participationStatus,
        CalendarEventKind eventKind,
        string? organizerName,
        string? organizerEmail,
        string? description,
        Uri? joinUrl,
        IReadOnlyList<CalendarEventParticipant>? participants)
    {
        Title = NormalizeTitle(title);
        Start = start;
        End = NormalizeEnd(end, start, isAllDay);
        IsAllDay = isAllDay;
        ParticipationStatus = participationStatus;
        EventKind = eventKind;
        OrganizerName = NormalizeOptionalText(organizerName);
        OrganizerEmail = NormalizeOptionalText(organizerEmail);
        Description = NormalizeDescription(description);
        JoinUrl = NormalizeJoinUrl(joinUrl);
        Participants = participants?.OfType<CalendarEventParticipant>().ToArray() ?? [];
    }

    public string Title { get; }

    public DateTimeOffset Start { get; }

    public DateTimeOffset? End { get; }

    public DateTimeOffset EffectiveEnd => End ?? Start.Add(GetDefaultDuration(IsAllDay));

    public bool IsAllDay { get; }

    public CalendarParticipationStatus ParticipationStatus { get; }

    public CalendarEventKind EventKind { get; }

    public string? OrganizerName { get; }

    public string? OrganizerEmail { get; }

    public string? Description { get; }

    public Uri? JoinUrl { get; }

    public IReadOnlyList<CalendarEventParticipant> Participants { get; }

    public string SafeTitle => Title;

    public string? OrganizerDisplayLabel =>
        !string.IsNullOrWhiteSpace(OrganizerName)
            ? OrganizerName.Trim()
            : !string.IsNullOrWhiteSpace(OrganizerEmail)
                ? OrganizerEmail.Trim()
                : null;

    /// <summary>
    /// Determines whether the event intersects the specified time range.
    /// </summary>
    /// <param name="rangeStart">The start of the range.</param>
    /// <param name="rangeEnd">The end of the range.</param>
    /// <returns><see langword="true"/> when the event overlaps the range; otherwise <see langword="false"/>.</returns>
    public bool Intersects(DateTimeOffset rangeStart, DateTimeOffset rangeEnd)
    {
        if (rangeEnd <= rangeStart)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rangeEnd),
                rangeEnd,
                "Range end must be greater than range start.");
        }

        return Start < rangeEnd && EffectiveEnd > rangeStart;
    }

    /// <summary>
    /// Normalizes an event title into a non-empty display-safe value.
    /// </summary>
    /// <param name="title">The source title.</param>
    /// <returns>The normalized title.</returns>
    private static string NormalizeTitle(string title) =>
        string.IsNullOrWhiteSpace(title)
            ? "Untitled event"
            : title.Trim();

    /// <summary>
    /// Normalizes optional organizer fields into trimmed nullable values.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <returns>The trimmed value, or <see langword="null"/> when blank.</returns>
    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    /// <summary>
    /// Normalizes the event description and collapses line endings to the internal format.
    /// </summary>
    /// <param name="value">The source description.</param>
    /// <returns>The normalized description, or <see langword="null"/> when blank.</returns>
    private static string? NormalizeDescription(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Trim();

    /// <summary>
    /// Accepts only absolute meeting links for event join URLs.
    /// </summary>
    /// <param name="joinUrl">The source join URL.</param>
    /// <returns>The validated absolute URI, or <see langword="null"/> when invalid.</returns>
    private static Uri? NormalizeJoinUrl(Uri? joinUrl) =>
        joinUrl is { IsAbsoluteUri: true }
            ? joinUrl
            : null;

    /// <summary>
    /// Normalizes the event end instant so that it always falls after the start.
    /// </summary>
    /// <param name="end">The source end instant.</param>
    /// <param name="start">The event start instant.</param>
    /// <param name="isAllDay">Whether the event spans the whole day.</param>
    /// <returns>The normalized end instant, or <see langword="null"/> when no explicit end exists.</returns>
    private static DateTimeOffset? NormalizeEnd(
        DateTimeOffset? end,
        DateTimeOffset start,
        bool isAllDay)
    {
        if (end is null)
        {
            return null;
        }

        return end > start
            ? end
            : start.Add(GetDefaultDuration(isAllDay));
    }

    /// <summary>
    /// Returns the fallback duration used when the source event omits a valid end instant.
    /// </summary>
    /// <param name="isAllDay">Whether the event spans the whole day.</param>
    /// <returns>The fallback duration.</returns>
    private static TimeSpan GetDefaultDuration(bool isAllDay) =>
        isAllDay
            ? TimeSpan.FromDays(1)
            : TimeSpan.FromMinutes(30);
}
