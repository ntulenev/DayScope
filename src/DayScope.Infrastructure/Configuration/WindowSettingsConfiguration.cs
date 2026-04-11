using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;

namespace DayScope.Infrastructure.Configuration;

/// <summary>
/// Applies normalization and validation rules to <see cref="WindowSettings"/>.
/// </summary>
public sealed class WindowSettingsConfiguration :
    IPostConfigureOptions<WindowSettings>,
    IValidateOptions<WindowSettings>
{
    /// <summary>
    /// Normalizes bound window settings after configuration binding.
    /// </summary>
    /// <param name="name">The options instance name.</param>
    /// <param name="options">The options instance to normalize.</param>
    public void PostConfigure(string? name, WindowSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Normalize();
    }

    /// <summary>
    /// Validates the normalized window settings.
    /// </summary>
    /// <param name="name">The options instance name.</param>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>The validation result.</returns>
    public ValidateOptionsResult Validate(string? name, WindowSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = options.Validate();
        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
