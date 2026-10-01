using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SendToKindle.Services
{
    /// <summary>
    /// Service to automatically inject the SendToKindle JavaScript into the Jellyfin Web UI.
    /// </summary>
    public class WebInjectorService : IHostedService
    {
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger<WebInjectorService> _logger;

        public WebInjectorService(IApplicationPaths appPaths, ILogger<WebInjectorService> logger)
        {
            _appPaths = appPaths;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
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
                    return Task.CompletedTask;
                }

                _logger.LogInformation("Found Jellyfin Web index.html at {Path}", indexPath);
                string content = File.ReadAllText(indexPath);

                var version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.8";
                string newTag = $"<script src=\"https://raw.githubusercontent.com/xenomorphik/jellyfin-plugin-sendtokindle/main/Jellyfin.Plugin.SendToKindle/Web/sendtokindle.js?v={version}\" defer></script>";

                bool modified = false;

                // Remove any old injection tags that don't match exactly
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
                        content = content.Replace(match.Value, string.Empty);
                        modified = true;
                    }
                }

                if (!hasExactMatch)
                {
                    content = content.Replace("</body>", $"    {newTag}\n</body>");
                    modified = true;
                }

                if (modified)
                {
                    File.WriteAllText(indexPath, content);
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

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
