namespace DayScope.Application.DaySchedule;

/// <summary>
/// Represents a participant row shown in the event details overlay.
/// </summary>
/// <param name="DisplayName">The participant label shown to the user.</param>
/// <param name="StatusLabel">The participant response label.</param>
/// <param name="IsSelf">Whether the participant represents the signed-in user.</param>
public sealed record EventParticipantDisplayState(
    string DisplayName,
    string StatusLabel,
    bool IsSelf);
