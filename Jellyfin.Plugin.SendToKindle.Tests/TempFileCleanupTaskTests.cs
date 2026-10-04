using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SendToKindle.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.SendToKindle.Tests
{
    public class TempFileCleanupTaskTests
    {
        [Fact]
        public async Task ExecuteAsync_ShouldDeleteOrphanedFilesOlderThanOneHour()
        {
            // Arrange
            var logger = NullLogger<TempFileCleanupTask>.Instance;
            var task = new TempFileCleanupTask(logger);
            
            var tempDir = Path.GetTempPath();
            var recentFile = Path.Combine(tempDir, "sendtokindle_recent.epub");
            var oldFile = Path.Combine(tempDir, "sendtokindle_old.epub");

            await File.WriteAllTextAsync(recentFile, "test", CancellationToken.None);
            await File.WriteAllTextAsync(oldFile, "test", CancellationToken.None);

            // Make the old file look old
            File.SetLastWriteTimeUtc(oldFile, DateTime.UtcNow.Subtract(TimeSpan.FromHours(2)));

            // Act
            await task.ExecuteAsync(new Progress<double>(), CancellationToken.None);

            // Assert
            Assert.True(File.Exists(recentFile));
            Assert.False(File.Exists(oldFile));

            // Cleanup
            if (File.Exists(recentFile))
            {
                File.Delete(recentFile);
            }
        }
    }
}
