namespace DayScope.Domain.Configuration;

public sealed class DemoModeSettings
{
    public bool Enabled { get; set; }

    public int UnreadEmailCount { get; set; } = 18;
}
