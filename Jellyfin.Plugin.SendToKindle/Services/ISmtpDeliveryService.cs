using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.SendToKindle.Services
{
    /// <summary>
    /// Interface for the SMTP delivery service.
    /// </summary>
    public interface ISmtpDeliveryService
    {
        /// <summary>
        /// Sends a book to the configured Kindle email address.
        /// </summary>
        /// <param name="bookFilePath">The path to the physical book file.</param>
        /// <param name="bookTitle">The title of the book.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation. Returns true if successful.</returns>
        Task<bool> SendBookAsync(string bookFilePath, string bookTitle, CancellationToken cancellationToken);
    }
}
