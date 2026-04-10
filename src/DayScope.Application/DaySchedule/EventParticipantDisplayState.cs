namespace DayScope.Application.DaySchedule;

public sealed record EventParticipantDisplayState(
    string DisplayName,
    string StatusLabel,
    bool IsSelf);
