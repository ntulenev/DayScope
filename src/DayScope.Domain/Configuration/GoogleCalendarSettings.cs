namespace DayScope.Domain.Configuration;

/// <summary>
/// Represents settings used to connect to Google Calendar and Gmail.
/// </summary>
public sealed class GoogleCalendarSettings
{
    public bool Enabled { get; set; } = true;

    public string CalendarId { get; set; } = "primary";

    public int RefreshMinutes { get; set; } = 5;

    public string ClientSecretsPath { get; set; } =
        @"%LocalAppData%\DayScope\google-oauth-client.json";

    public string TokenStoreDirectory { get; set; } =
        @"%LocalAppData%\DayScope\GoogleCalendarToken";

    public string? LoginHint { get; set; }

    public bool ForceAccountSelection { get; set; } = true;

    /// <summary>
    /// Normalizes the settings into supported ranges and formats.
    /// </summary>
    public void Normalize()
    {
        CalendarId = string.IsNullOrWhiteSpace(CalendarId)
            ? "primary"
            : CalendarId.Trim();
        RefreshMinutes = Math.Clamp(RefreshMinutes, 1, 60);
        ClientSecretsPath = ClientSecretsPath?.Trim() ?? string.Empty;
        TokenStoreDirectory = TokenStoreDirectory?.Trim() ?? string.Empty;
        LoginHint = string.IsNullOrWhiteSpace(LoginHint)
            ? null
            : LoginHint.Trim();
    }

    /// <summary>
    /// Validates the current settings and returns any failures.
    /// </summary>
    /// <returns>A list of validation failures.</returns>
    public IReadOnlyList<string> Validate()
    {
        List<string> failures = [];

        if (Enabled && string.IsNullOrWhiteSpace(CalendarId))
        {
            failures.Add("GoogleCalendar:CalendarId must be configured.");
        }

        return failures;
    }
}
