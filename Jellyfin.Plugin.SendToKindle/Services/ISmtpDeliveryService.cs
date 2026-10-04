using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.SendToKindle.Services;

/// <summary>
/// Interface for sending files via SMTP.
/// </summary>
public interface ISmtpDeliveryService
{
    /// <summary>
    /// Sends a book file to the configured Kindle email.
    /// </summary>
    /// <param name="bookFilePath">Path to the book file.</param>
    /// <param name="bookTitle">Title of the book.</param>
    /// <param name="targetEmail">The target email address for this user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation. Returns true if successful.</returns>
    Task<bool> SendBookAsync(string bookFilePath, string bookTitle, string targetEmail, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a test email to verify SMTP configuration.
    /// </summary>
    /// <param name="smtpServer">The SMTP server.</param>
    /// <param name="smtpPort">The SMTP port.</param>
    /// <param name="smtpUsername">The SMTP username.</param>
    /// <param name="smtpPassword">The SMTP password.</param>
    /// <param name="targetEmail">The target email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation. Returns true if successful.</returns>
    Task<bool> SendTestEmailAsync(string smtpServer, int smtpPort, string smtpUsername, string smtpPassword, string targetEmail, CancellationToken cancellationToken);
}
