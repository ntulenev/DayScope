using DayScope.Application.Abstractions;

namespace DayScope.ViewModels;

/// <summary>
/// Carries an updated inbox snapshot.
/// </summary>
public sealed class EmailInboxSnapshotChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailInboxSnapshotChangedEventArgs"/> class.
    /// </summary>
    /// <param name="snapshot">The updated inbox snapshot.</param>
    public EmailInboxSnapshotChangedEventArgs(EmailInboxSnapshot snapshot)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    /// <summary>
    /// Gets the updated inbox snapshot.
    /// </summary>
    public EmailInboxSnapshot Snapshot { get; }
}
