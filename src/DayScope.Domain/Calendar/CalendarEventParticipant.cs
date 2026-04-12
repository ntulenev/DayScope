namespace DayScope.Domain.Calendar;

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
