using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SendToKindle.Services;

/// <summary>
/// Implementation of the Kindle extraction service.
/// </summary>
public class KindleExtractionService : IKindleExtractionService
{
    private readonly ILibraryManager _libraryManager;
    private readonly ISmtpDeliveryService _smtpService;
    private readonly ILogger<KindleExtractionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KindleExtractionService"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="smtpService">The smtp delivery service.</param>
    /// <param name="logger">The logger.</param>
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
    public async Task<bool> SendItemToKindleAsync(Guid itemId, Guid userId, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            _logger.LogError("Plugin configuration is null.");
            return false;
        }

        var userMap = config.UserTargetKindleEmails.FirstOrDefault(u => u.UserId == userId.ToString());
        var targetEmail = userMap?.Email;

        if (string.IsNullOrEmpty(targetEmail))
        {
            targetEmail = config.TargetKindleEmail;
        }

        if (string.IsNullOrWhiteSpace(targetEmail))
        {
            _logger.LogWarning("No Target Kindle Email configured for user {UserId}", userId);
            return false;
        }

        var item = _libraryManager.GetItemById(itemId);
        if (item == null)
        {
            _logger.LogWarning("Item with ID {Id} not found.", itemId);
            return false;
        }

        if (!(item is Book book))
        {
            _logger.LogWarning("Item '{Name}' is not a Book. Only books can be sent to Kindle.", item.Name);
            return false;
        }

        var bookPath = FindBestFormat(item.Path);
        bool isConverted = false;

        string targetForDrm = bookPath ?? (File.Exists(item.Path) ? item.Path : (Directory.Exists(item.Path) ? Directory.GetFiles(item.Path).FirstOrDefault(f => Path.GetExtension(f).Equals(".mobi", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(f).Equals(".azw3", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(f).Equals(".epub", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(f).Equals(".pdf", StringComparison.OrdinalIgnoreCase)) : null)) ?? item.Path;

        if (!string.IsNullOrEmpty(targetForDrm) && File.Exists(targetForDrm))
        {
            var noDrm = TryRemoveDrm(targetForDrm);
            if (!string.IsNullOrEmpty(noDrm) && !string.Equals(noDrm, targetForDrm, StringComparison.OrdinalIgnoreCase))
            {
                if (bookPath != null)
                {
                    bookPath = noDrm;
                }
                else
                {
                    targetForDrm = noDrm; // Pass down the DRM-free path for conversion
                }
            }
        }

        if (string.IsNullOrEmpty(bookPath))
        {
            bookPath = ConvertToEpubIfPossible(targetForDrm);
            if (string.IsNullOrEmpty(bookPath))
            {
                _logger.LogWarning("No supported Kindle formats found for book '{Name}'.", item.Name);
                throw new InvalidOperationException("Unsupported format. Only EPUB, CBZ, CBR, PDF are supported.");
            }

            isConverted = true;
        }

        var fileInfo = new FileInfo(bookPath);
        if (fileInfo.Length > 50 * 1024 * 1024)
        {
            _logger.LogWarning("File size {Size} exceeds Amazon's 50MB Send to Kindle limit.", fileInfo.Length);
            throw new InvalidOperationException("File size exceeds Amazon's 50MB Send to Kindle limit.");
        }

        _logger.LogInformation("Found compatible format for '{Name}' at {Path}", item.Name, bookPath);

        var dto = new EpubMetadataDto
        {
            Title = item.Name,
            Series = book.SeriesName
        };

        var people = _libraryManager.GetPeople(item);
        if (people != null)
        {
            var authors = people.Where(p => string.Equals(p.Type.ToString(), "Author", StringComparison.OrdinalIgnoreCase)).Select(p => p.Name).ToList();
            if (authors.Count > 0)
            {
                foreach (var a in authors)
                {
                    dto.Authors.Add(a);
                }
            }
            else
            {
                dto.Authors.Add("Unknown Author");
            }
        }

        if (item.Tags != null)
        {
            foreach (var t in item.Tags)
            {
                dto.Subjects.Add(t);
            }
        }

        if (item.Genres != null)
        {
            foreach (var g in item.Genres)
            {
                dto.Subjects.Add(g);
            }
        }

        if (item.HasImage(MediaBrowser.Model.Entities.ImageType.Primary))
        {
            dto.CoverImagePath = item.GetImagePath(MediaBrowser.Model.Entities.ImageType.Primary);
        }

        // Fire and forget SMTP sending to prevent UI hangs.
        // We will run this in a background thread.
        _ = Task.Run(
            async () =>
        {
            string finalBookPath = bookPath;
            bool isTempFile = false;
            try
            {
                finalBookPath = EpubMetadataInjector.InjectMetadata(bookPath, dto, _logger);
                isTempFile = finalBookPath != bookPath;

                await _smtpService.SendBookAsync(finalBookPath, item.Name, targetEmail, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background Kindle send failed for {ItemName}", item.Name);
            }
            finally
            {
                if (isConverted && File.Exists(bookPath))
                {
                    try
                    {
                        File.Delete(bookPath);
                    }
                    catch
                    {
                    }
                }

                // Temp files are now managed by a background cleanup task, but we can also clean them up here.
                if (isTempFile && File.Exists(finalBookPath))
                {
                    try
                    {
                        File.Delete(finalBookPath);
                    }
                    catch
                    {
                    }
                }
            }
        },
            CancellationToken.None);

        // Always return true to acknowledge the queueing
        return true;
    }

    private string? FindBestFormat(string directoryOrFilePath)
    {
        if (File.Exists(directoryOrFilePath))
        {
            var ext = Path.GetExtension(directoryOrFilePath).ToLowerInvariant();
            if (ext == ".epub")
            {
                return directoryOrFilePath;
            }

            directoryOrFilePath = Path.GetDirectoryName(directoryOrFilePath) ?? string.Empty;
        }

        if (!Directory.Exists(directoryOrFilePath))
        {
            return null;
        }

        var files = Directory.GetFiles(directoryOrFilePath);
        var epub = files.FirstOrDefault(f => Path.GetExtension(f).Equals(".epub", StringComparison.OrdinalIgnoreCase));
        if (epub != null)
        {
            return epub;
        }

        return null;
    }

    private string? ConvertToEpubIfPossible(string directoryOrFilePath)
    {
        string? targetFile = null;

        if (File.Exists(directoryOrFilePath))
        {
            targetFile = directoryOrFilePath;
        }
        else if (Directory.Exists(directoryOrFilePath))
        {
            var files = Directory.GetFiles(directoryOrFilePath);
            targetFile = files.FirstOrDefault(f =>
                Path.GetExtension(f).Equals(".cbz", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(f).Equals(".cbr", StringComparison.OrdinalIgnoreCase));
        }

        if (string.IsNullOrEmpty(targetFile))
        {
            return null;
        }

        var ext = Path.GetExtension(targetFile).ToLowerInvariant();
        if (ext == ".cbz")
        {
            return SimpleCbzToEpub(targetFile);
        }

        if (ext == ".pdf" || ext == ".cbr" || ext == ".mobi" || ext == ".azw3")
        {
            return TryExternalCalibreConversion(targetFile);
        }

        return null;
    }

    private string? TryExternalCalibreConversion(string sourcePath)
    {
        try
        {
            var tempEpub = Path.Combine(Path.GetTempPath(), "sendtokindle_" + Guid.NewGuid().ToString() + ".epub");
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ebook-convert",
                    Arguments = $"\"{sourcePath}\" \"{tempEpub}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(60000); // 60 seconds timeout

            if (process.ExitCode == 0 && File.Exists(tempEpub))
            {
                _logger.LogInformation("Successfully converted {Source} to EPUB using Calibre.", sourcePath);
                return tempEpub;
            }
            else
            {
                _logger.LogWarning("Calibre conversion failed or timed out for {Source}. Exit code: {Code}", sourcePath, process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Calibre ebook-convert is likely not installed or not in PATH.");
        }

        return null;
    }

    private string? SimpleCbzToEpub(string cbzPath)
    {
        try
        {
            var tempEpub = Path.Combine(Path.GetTempPath(), "sendtokindle_" + Guid.NewGuid().ToString() + ".epub");
            using (var archive = ZipFile.OpenRead(cbzPath))
            using (var epub = ZipFile.Open(tempEpub, ZipArchiveMode.Create))
            {
                var mimetype = epub.CreateEntry("mimetype", CompressionLevel.NoCompression);
                using (var writer = new StreamWriter(mimetype.Open()))
                {
                    writer.Write("application/epub+zip");
                }

                var container = epub.CreateEntry("META-INF/container.xml");
                using (var writer = new StreamWriter(container.Open()))
                {
                    writer.Write("<?xml version=\"1.0\"?><container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\"><rootfiles><rootfile full-path=\"OEBPS/content.opf\" media-type=\"application/oebps-package+xml\"/></rootfiles></container>");
                }

                var opfContent = "<?xml version=\"1.0\"?><package version=\"3.0\" unique-identifier=\"BookId\" xmlns=\"http://www.idpf.org/2007/opf\"><metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\"><dc:title>Comic</dc:title><dc:language>en</dc:language></metadata><manifest>";
                var spineContent = "<spine>";

                int i = 0;
                var entries = archive.Entries.Where(e => e.FullName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || e.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || e.FullName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)).OrderBy(e => e.FullName).ToList();

                foreach (var entry in entries)
                {
                    var imgName = $"img_{i}.jpg";
                    var imgEntry = epub.CreateEntry($"OEBPS/Images/{imgName}");
                    using (var src = entry.Open())
                    using (var dst = imgEntry.Open())
                    {
                        src.CopyTo(dst);
                    }

                    opfContent += $"<item id=\"img{i}\" href=\"Images/{imgName}\" media-type=\"image/jpeg\"/>";

                    var htmlName = $"page_{i}.xhtml";
                    var htmlEntry = epub.CreateEntry($"OEBPS/Text/{htmlName}");
                    using (var writer = new StreamWriter(htmlEntry.Open()))
                    {
                        writer.Write($"<?xml version=\"1.0\" encoding=\"utf-8\"?><html xmlns=\"http://www.w3.org/1999/xhtml\"><head><title>Page {i}</title></head><body style=\"margin:0;padding:0;\"><img src=\"../Images/{imgName}\" style=\"width:100%;height:100%;object-fit:contain;\"/></body></html>");
                    }

                    opfContent += $"<item id=\"page{i}\" href=\"Text/{htmlName}\" media-type=\"application/xhtml+xml\"/>";
                    spineContent += $"<itemref idref=\"page{i}\"/>";
                    i++;
                }

                opfContent += "</manifest>";
                spineContent += "</spine></package>";

                var opfEntry = epub.CreateEntry("OEBPS/content.opf");
                using (var writer = new StreamWriter(opfEntry.Open()))
                {
                    writer.Write(opfContent + spineContent);
                }
            }

            return tempEpub;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert CBZ to EPUB.");
            return null;
        }
    }

    private string? TryRemoveDrm(string sourcePath)
    {
        try
        {
            var pluginPath = Path.Combine(Plugin.Instance?.AppPaths?.PluginsPath ?? string.Empty, Plugin.Instance?.Name ?? "SendToKindle", "DeDRM_plugin");
            if (!Directory.Exists(pluginPath))
            {
                // Fallback to searching the current assembly directory
                pluginPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty, "DeDRM_plugin");
                if (!Directory.Exists(pluginPath))
                {
                    return sourcePath;
                }
            }

            var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            string? scriptName = null;

            if (ext == ".mobi" || ext == ".azw3" || ext == ".prc")
            {
                scriptName = "mobidedrm.py";
            }
            else if (ext == ".epub")
            {
                scriptName = "ineptepub.py";
            }
            else if (ext == ".pdf")
            {
                scriptName = "ineptpdf.py";
            }
            else if (ext == ".pdb")
            {
                scriptName = "erdr2pml.py";
            }

            if (scriptName == null)
            {
                return sourcePath;
            }

            var scriptPath = Path.Combine(pluginPath, scriptName);
            if (!File.Exists(scriptPath))
            {
                return sourcePath;
            }

            var tempOut = Path.Combine(Path.GetTempPath(), "sendtokindle_nodrm_" + Guid.NewGuid().ToString() + ext);

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = $"\"{scriptPath}\" \"{sourcePath}\" \"{tempOut}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                process.WaitForExit(30000);
            }
            catch
            {
                process.StartInfo.FileName = "python";
                process.Start();
                process.WaitForExit(30000);
            }

            if (process.ExitCode == 0 && File.Exists(tempOut))
            {
                _logger.LogInformation("Successfully removed DRM from {Source} using DeDRM.", sourcePath);
                return tempOut;
            }
            else
            {
                var err = process.StandardError.ReadToEnd();
                _logger.LogWarning("DeDRM failed or timed out for {Source}. Exit code: {Code}. Error: {Error}", sourcePath, process.ExitCode, err);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "DeDRM execution failed. Is Python installed?");
        }

        return sourcePath;
    }
}
