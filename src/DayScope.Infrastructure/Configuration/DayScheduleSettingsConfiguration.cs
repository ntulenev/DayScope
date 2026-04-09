using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;

namespace DayScope.Infrastructure.Configuration;

public sealed class DayScheduleSettingsConfiguration :
    IPostConfigureOptions<DayScheduleSettings>,
    IValidateOptions<DayScheduleSettings>
{
    public void PostConfigure(string? name, DayScheduleSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.StartHour = Math.Clamp(options.StartHour, 0, 23);
        options.EndHour = Math.Clamp(options.EndHour, 1, 24);
        if (options.EndHour <= options.StartHour)
        {
            options.StartHour = 6;
            options.EndHour = 20;
        }

        options.HourHeight = Math.Clamp(options.HourHeight, 40, 160);
        options.ScheduleCanvasWidth = Math.Clamp(options.ScheduleCanvasWidth, 480, 1200);
        options.PrimaryTimeZoneLabel = string.IsNullOrWhiteSpace(options.PrimaryTimeZoneLabel)
            ? null
            : options.PrimaryTimeZoneLabel.Trim();
        options.SecondaryTimeZoneId = string.IsNullOrWhiteSpace(options.SecondaryTimeZoneId)
            ? null
            : options.SecondaryTimeZoneId.Trim();
        options.SecondaryTimeZoneLabel = string.IsNullOrWhiteSpace(options.SecondaryTimeZoneLabel)
            ? null
            : options.SecondaryTimeZoneLabel.Trim();
    }

    public ValidateOptionsResult Validate(string? name, DayScheduleSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.EndHour <= options.StartHour)
        {
            return ValidateOptionsResult.Fail("DaySchedule:EndHour must be greater than StartHour.");
        }

        return ValidateOptionsResult.Success;
    }
}
