using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SendToKindle.Services
{
    /// <summary>
    /// Implementation of the Kindle extraction service.
    /// </summary>
    public class KindleExtractionService : IKindleExtractionService
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ISmtpDeliveryService _smtpService;
        private readonly ILogger<KindleExtractionService> _logger;

        public KindleExtractionService(
            ILibraryManager libraryManager,
            ISmtpDeliveryService smtpService,
            ILogger<KindleExtractionService> logger)
        {
            _libraryManager = libraryManager;
            _smtpService = smtpService;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<bool> SendItemToKindleAsync(Guid itemId, CancellationToken cancellationToken)
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                _logger.LogWarning("Item with ID {Id} not found.", itemId);
                return false;
            }

            if (!(item is Book))
            {
                _logger.LogWarning("Item '{Name}' is not a Book. Only books can be sent to Kindle.", item.Name);
                return false;
            }

            var bookPath = FindBestFormat(item.Path);
            if (string.IsNullOrEmpty(bookPath))
            {
                _logger.LogWarning("No supported Kindle formats (EPUB, MOBI) found for book '{Name}'.", item.Name);
                return false;
            }

            _logger.LogInformation("Found compatible format for '{Name}' at {Path}", item.Name, bookPath);

            return await _smtpService.SendBookAsync(bookPath, item.Name, cancellationToken).ConfigureAwait(false);
        }

        private string? FindBestFormat(string directoryOrFilePath)
        {
            // If the item path is already a direct file to a supported format
            if (File.Exists(directoryOrFilePath))
            {
                var ext = Path.GetExtension(directoryOrFilePath).ToLowerInvariant();
                if (ext == ".epub" || ext == ".mobi")
                {
                    return directoryOrFilePath;
                }
                
                // If it's a file but unsupported (e.g. .pdf), try looking in its directory
                directoryOrFilePath = Path.GetDirectoryName(directoryOrFilePath) ?? string.Empty;
            }

            if (!Directory.Exists(directoryOrFilePath))
            {
                return null;
            }

            var files = Directory.GetFiles(directoryOrFilePath);

            // Prioritize EPUB
            var epub = files.FirstOrDefault(f => Path.GetExtension(f).Equals(".epub", StringComparison.OrdinalIgnoreCase));
            if (epub != null)
            {
                return epub;
            }

            // Fallback to MOBI
            var mobi = files.FirstOrDefault(f => Path.GetExtension(f).Equals(".mobi", StringComparison.OrdinalIgnoreCase));
            if (mobi != null)
            {
                return mobi;
            }

            return null;
        }
    }
}
