using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;

namespace DayScope.Infrastructure.Configuration;

public sealed class GoogleCalendarSettingsConfiguration :
    IPostConfigureOptions<GoogleCalendarSettings>,
    IValidateOptions<GoogleCalendarSettings>
{
    public void PostConfigure(string? name, GoogleCalendarSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.CalendarId = string.IsNullOrWhiteSpace(options.CalendarId)
            ? "primary"
            : options.CalendarId.Trim();
        options.RefreshMinutes = Math.Clamp(options.RefreshMinutes, 1, 60);
        options.ClientSecretsPath = options.ClientSecretsPath?.Trim() ?? string.Empty;
        options.TokenStoreDirectory = options.TokenStoreDirectory?.Trim() ?? string.Empty;
        options.LoginHint = string.IsNullOrWhiteSpace(options.LoginHint)
            ? null
            : options.LoginHint.Trim();
    }

    public ValidateOptionsResult Validate(string? name, GoogleCalendarSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Enabled && string.IsNullOrWhiteSpace(options.CalendarId))
        {
            return ValidateOptionsResult.Fail("GoogleCalendar:CalendarId must be configured.");
        }

        return ValidateOptionsResult.Success;
    }
}
