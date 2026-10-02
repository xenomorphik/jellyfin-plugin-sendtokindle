using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SendToKindle.Services;

/// <summary>
/// Service to automatically inject the SendToKindle JavaScript into the Jellyfin Web UI.
/// </summary>
public class WebInjectorService : IHostedService
{
    private readonly IApplicationPaths _appPaths;
    private readonly ILogger<WebInjectorService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebInjectorService"/> class.
    /// </summary>
    /// <param name="appPaths">The application paths.</param>
    /// <param name="logger">The logger.</param>
    public WebInjectorService(IApplicationPaths appPaths, ILogger<WebInjectorService> logger)
    {
        _appPaths = appPaths;
        _logger = logger;
    }

    /// <summary>
    /// Starts the service.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to inject SendToKindle script into Jellyfin Web UI...");

        try
        {
            string[] possiblePaths = new[]
            {
                Path.Combine(_appPaths.ProgramDataPath, "jellyfin-web"),
                "/usr/share/jellyfin/web",
                "/jellyfin/jellyfin-web",
                @"C:\Program Files\Jellyfin\Server\jellyfin-web"
            };

            string? indexPath = null;
            foreach (var path in possiblePaths)
            {
                var testPath = Path.Combine(path, "index.html");
                if (File.Exists(testPath))
                {
                    indexPath = testPath;
                    break;
                }
            }

            if (indexPath == null)
            {
                _logger.LogWarning("Could not find Jellyfin Web index.html. The SendToKindle context menu button will not appear unless manually injected.");
                return;
            }

            _logger.LogInformation("Found Jellyfin Web index.html at {Path}", indexPath);
            string content = await File.ReadAllTextAsync(indexPath, cancellationToken).ConfigureAwait(false);

            var version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.9";

            // Note: raw.githubusercontent.com serves files as text/plain and browsers will refuse to execute it.
            // Using jsdelivr CDN to properly serve as application/javascript.
            string newTag = $"<script src=\"https://cdn.jsdelivr.net/gh/xenomorphik/jellyfin-plugin-sendtokindle@v{version}/Jellyfin.Plugin.SendToKindle/Web/sendtokindle.js\" defer></script>";

            bool modified = false;

            var pattern = @"<script[^>]*sendtokindle\.js[^>]*></script>";
            var existingMatches = Regex.Matches(content, pattern);

            bool hasExactMatch = false;
            foreach (Match match in existingMatches)
            {
                if (match.Value == newTag)
                {
                    hasExactMatch = true;
                }
                else
                {
                    content = content.Replace(match.Value, string.Empty, StringComparison.Ordinal);
                    modified = true;
                }
            }

            if (!hasExactMatch)
            {
                content = content.Replace("</body>", $"    {newTag}\n</body>", StringComparison.Ordinal);
                modified = true;
            }

            if (modified)
            {
                await File.WriteAllTextAsync(indexPath, content, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Successfully updated SendToKindle script tag in Jellyfin Web UI.");
            }
            else
            {
                _logger.LogInformation("SendToKindle script is already up-to-date in Jellyfin Web UI.");
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError("Permission denied while trying to modify Jellyfin Web index.html. You must manually add the script tag to index.html or fix folder permissions.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inject SendToKindle script into Jellyfin Web UI.");
        }
    }

    /// <summary>
    /// Stops the service.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
