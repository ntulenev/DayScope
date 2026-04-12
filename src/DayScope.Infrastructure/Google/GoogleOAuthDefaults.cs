using Google.Apis.Calendar.v3;
using Google.Apis.Gmail.v1;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Centralizes the shared constants used by Google OAuth and API client wiring.
/// </summary>
internal static class GoogleOAuthDefaults
{
    /// <summary>
    /// Gets the stable token-store user identifier used by installed-app OAuth.
    /// </summary>
    internal static string TokenStoreUserId => "dayscope-google-services";

    /// <summary>
    /// Gets the application name reported to Google client libraries.
    /// </summary>
    internal static string ApplicationName => "DayScope";

    /// <summary>
    /// Gets the scopes required by the Google-backed infrastructure services.
    /// </summary>
    internal static IReadOnlyList<string> Scopes { get; } =
    [
        CalendarService.Scope.CalendarReadonly,
        GmailService.Scope.GmailReadonly
    ];
}
