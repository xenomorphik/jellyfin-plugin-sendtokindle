using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SendToKindle.Services;

/// <summary>
/// Service to inject the frontend JS into Jellyfin's web index.
/// </summary>
public class WebInjectorService : IHostedService
{
    private readonly ILogger<WebInjectorService> _logger;
    private readonly MediaBrowser.Model.IO.IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebInjectorService"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="logger">The logger.</param>
    public WebInjectorService(MediaBrowser.Model.IO.IFileSystem fileSystem, ILogger<WebInjectorService> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        InjectScript();
        return Task.CompletedTask;
    }

    private void InjectScript()
    {
        try
        {
            var webPath = Environment.GetEnvironmentVariable("JELLYFIN_WEB_DIR");
            if (string.IsNullOrEmpty(webPath))
            {
                webPath = "/usr/share/jellyfin/web"; // Default docker/linux path
            }

            if (!Directory.Exists(webPath))
            {
                _logger.LogWarning("Jellyfin web directory not found at {Path}. SendToKindle UI injection will not work.", webPath);
                return;
            }

            var indexPath = Path.Combine(webPath, "index.html");
            if (!File.Exists(indexPath))
            {
                _logger.LogWarning("index.html not found in {Path}. SendToKindle UI injection will not work.", webPath);
                return;
            }

            var html = File.ReadAllText(indexPath);

            var scriptPath = "/SendToKindle/sendtokindle.js";
            var injectString = $"<script src=\"{scriptPath}\"></script>";

            if (html.Contains(injectString, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("SendToKindle script already injected in index.html");
                return;
            }

            var headEnd = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
            if (headEnd != -1)
            {
                html = html.Insert(headEnd, injectString + "\n");
                File.WriteAllText(indexPath, html);
                _logger.LogInformation("Successfully injected SendToKindle script into Jellyfin Web UI.");
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

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
