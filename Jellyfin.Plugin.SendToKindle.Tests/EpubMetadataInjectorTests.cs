using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Jellyfin.Plugin.SendToKindle.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.SendToKindle.Tests
{
    public sealed class EpubMetadataInjectorTests : IDisposable
    {
        private readonly string _tempEpubPath;

        public EpubMetadataInjectorTests()
        {
            _tempEpubPath = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid().ToString() + ".epub");
            CreateDummyEpub(_tempEpubPath);
        }

        private void CreateDummyEpub(string path)
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                var mimetype = archive.CreateEntry("mimetype");
                using (var writer = new StreamWriter(mimetype.Open()))
                {
                    writer.Write("application/epub+zip");
                }

                var container = archive.CreateEntry("META-INF/container.xml");
                using (var writer = new StreamWriter(container.Open()))
                {
                    writer.Write("<?xml version=\"1.0\"?><container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\"><rootfiles><rootfile full-path=\"OEBPS/content.opf\" media-type=\"application/oebps-package+xml\"/></rootfiles></container>");
                }

                var opfContent = "<?xml version=\"1.0\"?><package version=\"3.0\" unique-identifier=\"BookId\" xmlns=\"http://www.idpf.org/2007/opf\"><metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\"><dc:title>Original Title</dc:title></metadata><manifest></manifest><spine></spine></package>";
                var opf = archive.CreateEntry("OEBPS/content.opf");
                using (var writer = new StreamWriter(opf.Open()))
                {
                    writer.Write(opfContent);
                }
            }
        }

        public void Dispose()
        {
            if (File.Exists(_tempEpubPath))
            {
                File.Delete(_tempEpubPath);
            }
        }

        [Fact]
        public void InjectMetadata_ShouldUpdateTitleAndAddAuthorsAndSubjects()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var dto = new EpubMetadataDto
            {
                Title = "Injected Title",
                Series = "Test Series"
            };
            dto.Authors.Add("Test Author");
            dto.Subjects.Add("Test Tag");

            // Act
            var newPath = EpubMetadataInjector.InjectMetadata(_tempEpubPath, dto, logger);

            // Assert
            Assert.NotEqual(_tempEpubPath, newPath);
            Assert.True(File.Exists(newPath));

            using (var archive = ZipFile.OpenRead(newPath))
            {
                var opfEntry = archive.GetEntry("OEBPS/content.opf");
                Assert.NotNull(opfEntry);

                using (var stream = opfEntry.Open())
                using (var reader = new StreamReader(stream))
                {
                    var content = reader.ReadToEnd();
                    Assert.Contains("<dc:title>Injected Title</dc:title>", content, StringComparison.Ordinal);
                    Assert.Contains("<dc:creator>Test Author</dc:creator>", content, StringComparison.Ordinal);
                    Assert.Contains("<dc:subject>Test Tag</dc:subject>", content, StringComparison.Ordinal);
                    Assert.Contains("content=\"Test Series\"", content, StringComparison.Ordinal);
                    Assert.Contains("name=\"calibre:series\"", content, StringComparison.Ordinal);
                }
            }

            if (File.Exists(newPath))
            {
                File.Delete(newPath);
            }
        }
    }
}
