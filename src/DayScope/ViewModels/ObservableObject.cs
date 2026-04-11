using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DayScope.ViewModels;

/// <summary>
/// Provides basic property change notification support for view models.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Updates a backing field and raises property change notifications when the value changes.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="field">The backing field to update.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The name of the changed property.</param>
    /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
    protected bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the changed property.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
