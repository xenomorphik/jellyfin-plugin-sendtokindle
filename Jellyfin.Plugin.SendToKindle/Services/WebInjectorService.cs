using System;
using System.IO;
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

        private const string InjectionTag = "<script src=\"https://raw.githubusercontent.com/xenomorphik/jellyfin-plugin-sendtokindle/main/Jellyfin.Plugin.SendToKindle/Web/sendtokindle.js\" defer></script>";

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
                // In many installations, the web path is located relative to the application paths
                // We'll try a few common locations
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

                if (!content.Contains("sendtokindle.js"))
                {
                    content = content.Replace("</body>", $"    {InjectionTag}\n</body>");
                    File.WriteAllText(indexPath, content);
                    _logger.LogInformation("Successfully injected SendToKindle script into Jellyfin Web UI.");
                }
                else
                {
                    _logger.LogInformation("SendToKindle script is already injected into Jellyfin Web UI.");
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
