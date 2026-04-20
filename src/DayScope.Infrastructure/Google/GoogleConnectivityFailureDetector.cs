using System.Net.Sockets;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Detects failures that are likely caused by a missing or unstable network connection.
/// </summary>
internal static class GoogleConnectivityFailureDetector
{
    /// <summary>
    /// Returns whether the supplied exception represents a transient connectivity failure.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns><see langword="true"/> when the failure looks network-related; otherwise <see langword="false"/>.</returns>
    public static bool IsConnectivityFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            HttpRequestException => true,
            SocketException => true,
            TaskCanceledException => true,
            TimeoutException => true,
            _ when exception.InnerException is not null => IsConnectivityFailure(exception.InnerException),
            _ => false
        };
    }
}
