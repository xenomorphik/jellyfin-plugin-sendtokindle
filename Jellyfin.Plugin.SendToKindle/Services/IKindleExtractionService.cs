using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.SendToKindle.Services;

/// <summary>
/// Service for extracting kindle books.
/// </summary>
public interface IKindleExtractionService
{
    /// <summary>
    /// Sends the item to kindle.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task.</returns>
    public Task<bool> SendItemToKindleAsync(Guid itemId, Guid userId, CancellationToken cancellationToken);
}
