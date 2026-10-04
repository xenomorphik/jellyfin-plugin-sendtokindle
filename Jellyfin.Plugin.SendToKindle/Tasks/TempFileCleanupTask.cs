using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SendToKindle.Tasks;

/// <summary>
/// A scheduled task to clean up orphaned temporary Send to Kindle files.
/// </summary>
public class TempFileCleanupTask : IScheduledTask
{
    private readonly ILogger<TempFileCleanupTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TempFileCleanupTask"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public TempFileCleanupTask(ILogger<TempFileCleanupTask> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Send to Kindle Temp File Cleanup";

    /// <inheritdoc />
    public string Key => "SendToKindleTempFileCleanup";

    /// <inheritdoc />
    public string Description => "Cleans up orphaned temporary EPUB files left behind by interrupted Send to Kindle deliveries.";

    /// <inheritdoc />
    public string Category => "Maintenance";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(24).Ticks
            }
        };
    }

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Send to Kindle temp file cleanup.");

        var tempPath = Path.GetTempPath();
        try
        {
            if (Directory.Exists(tempPath))
            {
                var files = Directory.GetFiles(tempPath, "sendtokindle_*.epub");
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc > TimeSpan.FromHours(1))
                        {
                            _logger.LogInformation("Deleting orphaned temp file: {Path}", file);
                            File.Delete(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp file {Path}", file);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during temp file cleanup.");
        }

        _logger.LogInformation("Send to Kindle temp file cleanup completed.");
        progress.Report(100);
        return Task.CompletedTask;
    }
}
