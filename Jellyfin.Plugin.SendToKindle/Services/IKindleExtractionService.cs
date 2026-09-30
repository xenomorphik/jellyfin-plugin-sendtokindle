using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;

namespace Jellyfin.Plugin.SendToKindle.Services
{
    /// <summary>
    /// Interface for extracting books to send to Kindle.
    /// </summary>
    public interface IKindleExtractionService
    {
        /// <summary>
        /// Handles the "Send to Kindle" action for a given item.
        /// </summary>
        /// <param name="itemId">The ID of the item to process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the operation. Returns true if successful.</returns>
        Task<bool> SendItemToKindleAsync(Guid itemId, CancellationToken cancellationToken);
    }
}
