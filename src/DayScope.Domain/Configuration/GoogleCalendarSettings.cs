namespace DayScope.Domain.Configuration;

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
}
