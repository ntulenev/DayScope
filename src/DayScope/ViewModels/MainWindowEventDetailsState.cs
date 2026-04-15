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
    /// Updates the signed-in Google account email used to build account-aware meeting links.
    /// </summary>
    /// <param name="emailAddress">The signed-in email address, if known.</param>
    public void ApplyGoogleAccountEmail(string? emailAddress)
    {
        var normalizedEmailAddress = NormalizeEmailAddress(emailAddress);
        if (string.Equals(
            _googleAccountEmail,
            normalizedEmailAddress,
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _googleAccountEmail = normalizedEmailAddress;
        SetSelectedEventDetails(BuildAccountAwareDetails(_selectedEventDetails));
    }

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

        SetSelectedEventDetails(BuildAccountAwareDetails(details));
    }

    /// <summary>
    /// Closes the details overlay.
    /// </summary>
    public void Close() => SetSelectedEventDetails(null);

    private EventDetailsDisplayState? BuildAccountAwareDetails(EventDetailsDisplayState? details)
    {
        if (details is null)
        {
            return null;
        }

        var accountAwareJoinUrl = BuildAccountAwareJoinUrl(details.JoinUrl, _googleAccountEmail);
        return accountAwareJoinUrl == details.JoinUrl
            ? details
            : details with { JoinUrl = accountAwareJoinUrl };
    }

    private static Uri? BuildAccountAwareJoinUrl(Uri? joinUrl, string? emailAddress)
    {
        if (joinUrl is not { IsAbsoluteUri: true } || !IsGoogleMeetUri(joinUrl))
        {
            return joinUrl;
        }

        var queryParameters = ParseQueryParameters(joinUrl.Query);
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            queryParameters.Remove(AUTHUSER_PARAMETER_NAME);
        }
        else
        {
            queryParameters[AUTHUSER_PARAMETER_NAME] = emailAddress.Trim();
        }

        var builder = new UriBuilder(joinUrl)
        {
            Query = BuildQueryString(queryParameters)
        };

        return builder.Uri;
    }

    private static bool IsGoogleMeetUri(Uri uri) =>
        uri.Host.Equals("meet.google.com", StringComparison.OrdinalIgnoreCase);

    private static Dictionary<string, string> ParseQueryParameters(string query)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return parameters;
        }

        foreach (var segment in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = segment.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                parameters[DecodeQueryValue(segment)] = string.Empty;
                continue;
            }

            parameters[DecodeQueryValue(segment[..separatorIndex])] =
                DecodeQueryValue(segment[(separatorIndex + 1)..]);
        }

        return parameters;
    }

    private static string BuildQueryString(Dictionary<string, string> queryParameters)
    {
        if (queryParameters.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            "&",
            queryParameters.Select(parameter => string.IsNullOrEmpty(parameter.Value)
                ? Uri.EscapeDataString(parameter.Key)
                : string.Concat(
                    Uri.EscapeDataString(parameter.Key),
                    "=",
                    Uri.EscapeDataString(parameter.Value))));
    }

    private static string DecodeQueryValue(string value) =>
        Uri.UnescapeDataString(value.Replace("+", "%20", StringComparison.Ordinal));

    private static string? NormalizeEmailAddress(string? emailAddress) =>
        string.IsNullOrWhiteSpace(emailAddress)
            ? null
            : emailAddress.Trim();

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
    private string? _googleAccountEmail;
    private const string AUTHUSER_PARAMETER_NAME = "authuser";
}
