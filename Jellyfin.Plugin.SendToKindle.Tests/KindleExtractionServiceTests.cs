using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SendToKindle.Configuration;
using Jellyfin.Plugin.SendToKindle.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.SendToKindle.Tests
{
    public class KindleExtractionServiceTests
    {
        private class TestItem : BaseItem
        {
        }

        [Fact]
        public async Task SendItemToKindleAsync_ShouldFailIfItemNotBook()
        {
            // Arrange
            var libraryManagerMock = new Mock<ILibraryManager>();
            var smtpServiceMock = new Mock<ISmtpDeliveryService>();
            var logger = NullLogger<KindleExtractionService>.Instance;

            var itemId = Guid.NewGuid();
            var item = new TestItem { Id = itemId, Name = "Test Item" };
            libraryManagerMock.Setup(m => m.GetItemById(itemId)).Returns(item);

            var service = new KindleExtractionService(libraryManagerMock.Object, smtpServiceMock.Object, logger);

            // Act
            var result = await service.SendItemToKindleAsync(itemId, Guid.NewGuid(), CancellationToken.None);

            // Assert
            Assert.False(result);
            smtpServiceMock.Verify(m => m.SendBookAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
