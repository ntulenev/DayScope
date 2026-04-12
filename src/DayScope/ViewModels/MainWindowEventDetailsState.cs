using DayScope.Application.DaySchedule;

namespace DayScope.ViewModels;

/// <summary>
/// Stores the selected-event overlay state for the main window.
/// </summary>
public sealed class MainWindowEventDetailsState : ObservableObject
{
    public EventDetailsDisplayState? SelectedEventDetails => _selectedEventDetails;

    public bool IsOpen => SelectedEventDetails is not null;

    public bool HasOrganizer => !string.IsNullOrWhiteSpace(SelectedEventDetails?.Organizer);

    public bool HasDescription => !string.IsNullOrWhiteSpace(SelectedEventDetails?.Description);

    public bool HasParticipants => SelectedEventDetails?.Participants.Count > 0;

    public bool HasJoinUrl => SelectedEventDetails?.JoinUrl is not null;

    public string JoinLabel =>
        SelectedEventDetails?.JoinUrl?.Host.Contains("meet.google.com", StringComparison.OrdinalIgnoreCase) is true
            ? "Join Google Meet"
            : "Open meeting link";

    /// <summary>
    /// Opens the details overlay for the provided event state.
    /// </summary>
    /// <param name="eventState">The selected timed or all-day event display state.</param>
    public void Open(object? eventState)
    {
        var details = eventState switch
        {
            TimedEventDisplayState timedEvent => timedEvent.Details,
            AllDayEventDisplayState allDayEvent => allDayEvent.Details,
            _ => null
        };

        SetSelectedEventDetails(details);
    }

    /// <summary>
    /// Closes the details overlay.
    /// </summary>
    public void Close() => SetSelectedEventDetails(null);

    private void SetSelectedEventDetails(EventDetailsDisplayState? details)
    {
        if (!SetProperty(ref _selectedEventDetails, details, nameof(SelectedEventDetails)))
        {
            return;
        }

        OnPropertyChanged(nameof(IsOpen));
        OnPropertyChanged(nameof(HasOrganizer));
        OnPropertyChanged(nameof(HasDescription));
        OnPropertyChanged(nameof(HasParticipants));
        OnPropertyChanged(nameof(HasJoinUrl));
        OnPropertyChanged(nameof(JoinLabel));
    }

    private EventDetailsDisplayState? _selectedEventDetails;
}
