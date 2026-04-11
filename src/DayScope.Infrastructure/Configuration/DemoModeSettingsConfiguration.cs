using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;

namespace DayScope.Infrastructure.Configuration;

/// <summary>
/// Applies normalization and validation rules to <see cref="DemoModeSettings"/>.
/// </summary>
public sealed class DemoModeSettingsConfiguration :
    IPostConfigureOptions<DemoModeSettings>,
    IValidateOptions<DemoModeSettings>
{
    /// <summary>
    /// Normalizes bound demo mode settings after configuration binding.
    /// </summary>
    /// <param name="name">The options instance name.</param>
    /// <param name="options">The options instance to normalize.</param>
    public void PostConfigure(string? name, DemoModeSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Normalize();
    }

    /// <summary>
    /// Validates the normalized demo mode settings.
    /// </summary>
    /// <param name="name">The options instance name.</param>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>The validation result.</returns>
    public ValidateOptionsResult Validate(string? name, DemoModeSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = options.Validate();
        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
