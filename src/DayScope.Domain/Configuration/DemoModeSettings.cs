namespace DayScope.Domain.Configuration;

/// <summary>
/// Represents settings for demo mode.
/// </summary>
public sealed class DemoModeSettings
{
    public bool Enabled { get; set; }

    public int UnreadEmailCount { get; set; } = 18;

    /// <summary>
    /// Normalizes demo mode values into supported ranges.
    /// </summary>
    public void Normalize() => UnreadEmailCount = Math.Max(0, UnreadEmailCount);

    /// <summary>
    /// Validates the current demo mode settings.
    /// </summary>
    /// <returns>A list of validation failures.</returns>
    public IReadOnlyList<string> Validate()
    {
        List<string> failures = [];

        if (UnreadEmailCount < 0)
        {
            failures.Add("DemoMode:UnreadEmailCount must be greater than or equal to zero.");
        }

        return failures;
    }
}
