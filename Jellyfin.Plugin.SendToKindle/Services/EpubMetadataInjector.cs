using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SendToKindle.Services;

/// <summary>
/// Injects Jellyfin metadata into EPUB files.
/// </summary>
public static class EpubMetadataInjector
{
    /// <summary>
    /// Injects metadata into a copy of the EPUB file.
    /// </summary>
    /// <param name="sourceEpubPath">The source EPUB path.</param>
    /// <param name="metadataDto">The metadata.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The path to the modified EPUB file.</returns>
    public static string InjectMetadata(string sourceEpubPath, EpubMetadataDto metadataDto, ILogger logger)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "sendtokindle_" + Guid.NewGuid().ToString() + ".epub");
        File.Copy(sourceEpubPath, tempPath);

        try
        {
            using (var archive = ZipFile.Open(tempPath, ZipArchiveMode.Update))
            {
                var containerEntry = archive.GetEntry("META-INF/container.xml") ?? archive.Entries.FirstOrDefault(e => e.FullName.Equals("META-INF/container.xml", StringComparison.OrdinalIgnoreCase));
                if (containerEntry == null)
                {
                    logger.LogWarning("META-INF/container.xml not found in EPUB. Cannot inject metadata.");
                    return tempPath;
                }

                string opfPath = string.Empty;
                try
                {
                    using (var containerStream = containerEntry.Open())
                    {
                        var containerDoc = XDocument.Load(containerStream);
                        var rootfile = containerDoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "rootfile" && e.Attribute("media-type")?.Value == "application/oebps-package+xml");
                        opfPath = rootfile?.Attribute("full-path")?.Value ?? string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse container.xml.");
                }

                if (string.IsNullOrEmpty(opfPath))
                {
                    logger.LogWarning("OPF path not found in container.xml. Cannot inject metadata.");
                    return tempPath;
                }

                var opfEntry = archive.GetEntry(opfPath) ?? archive.Entries.FirstOrDefault(e => e.FullName.Equals(opfPath, StringComparison.OrdinalIgnoreCase));
                if (opfEntry == null)
                {
                    logger.LogWarning("OPF file not found at {OpfPath}. Cannot inject metadata.", opfPath);
                    return tempPath;
                }

                XDocument opfDoc;
                try
                {
                    using (var opfStream = opfEntry.Open())
                    {
                        opfDoc = XDocument.Load(opfStream);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse OPF file.");
                    return tempPath;
                }

                var opfNs = opfDoc.Root?.Name.Namespace ?? "http://www.idpf.org/2007/opf";
                var metadata = opfDoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "metadata");
                var manifest = opfDoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "manifest");

                if (metadata != null)
                {
                    XNamespace dcNs = "http://purl.org/dc/elements/1.1/";

                    if (!string.IsNullOrEmpty(metadataDto.Title))
                    {
                        var titleElem = metadata.Elements().FirstOrDefault(e => e.Name.LocalName == "title");
                        if (titleElem != null)
                        {
                            titleElem.Value = metadataDto.Title;
                        }
                        else
                        {
                            metadata.Add(new XElement(dcNs + "title", metadataDto.Title));
                        }
                    }

                    if (metadataDto.Authors != null && metadataDto.Authors.Count > 0)
                    {
                        var creators = metadata.Elements().Where(e => e.Name.LocalName == "creator").ToList();
                        foreach (var c in creators)
                        {
                            c.Remove();
                        }

                        foreach (var author in metadataDto.Authors)
                        {
                            metadata.Add(new XElement(dcNs + "creator", author));
                        }
                    }

                    if (metadataDto.Subjects != null && metadataDto.Subjects.Count > 0)
                    {
                        var subjects = metadata.Elements().Where(e => e.Name.LocalName == "subject").ToList();
                        foreach (var s in subjects)
                        {
                            s.Remove();
                        }

                        foreach (var subject in metadataDto.Subjects)
                        {
                            metadata.Add(new XElement(dcNs + "subject", subject));
                        }
                    }

                    if (!string.IsNullOrEmpty(metadataDto.Series))
                    {
                        var seriesMeta = metadata.Elements().FirstOrDefault(e => e.Name.LocalName == "meta" && e.Attribute("name")?.Value == "calibre:series");
                        if (seriesMeta != null)
                        {
                            seriesMeta.SetAttributeValue("content", metadataDto.Series);
                        }
                        else
                        {
                            metadata.Add(new XElement(opfNs + "meta", new XAttribute("name", "calibre:series"), new XAttribute("content", metadataDto.Series)));
                        }
                    }

                    if (!string.IsNullOrEmpty(metadataDto.CoverImagePath) && File.Exists(metadataDto.CoverImagePath))
                    {
                        string coverId = "jellyfin-cover-image";
                        string coverFileName = "jellyfin-cover" + Path.GetExtension(metadataDto.CoverImagePath);

                        string opfDir = Path.GetDirectoryName(opfPath)?.Replace('\\', '/') ?? string.Empty;
                        if (!string.IsNullOrEmpty(opfDir) && !opfDir.EndsWith('/'))
                        {
                            opfDir += "/";
                        }

                        string coverZipPath = opfDir + coverFileName;

                        var existingCover = archive.GetEntry(coverZipPath);
                        existingCover?.Delete();

                        archive.CreateEntryFromFile(metadataDto.CoverImagePath, coverZipPath);

                        if (manifest != null)
                        {
                            var existingItem = manifest.Elements().FirstOrDefault(e => e.Name.LocalName == "item" && e.Attribute("id")?.Value == coverId);
                            if (existingItem == null)
                            {
                                manifest.Add(new XElement(
                                    opfNs + "item",
                                    new XAttribute("id", coverId),
                                    new XAttribute("href", coverFileName),
                                    new XAttribute("media-type", "image/jpeg")));
                            }
                        }

                        var metaCover = metadata.Elements().FirstOrDefault(e => e.Name.LocalName == "meta" && e.Attribute("name")?.Value == "cover");
                        if (metaCover != null)
                        {
                            metaCover.SetAttributeValue("content", coverId);
                        }
                        else
                        {
                            metadata.Add(new XElement(
                                opfNs + "meta",
                                new XAttribute("name", "cover"),
                                new XAttribute("content", coverId)));
                        }
                    }
                }

                opfEntry.Delete();
                var newOpfEntry = archive.CreateEntry(opfPath);
                using (var newOpfStream = newOpfEntry.Open())
                {
                    opfDoc.Save(newOpfStream);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to inject metadata into EPUB.");
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            File.Copy(sourceEpubPath, tempPath);
        }

        return tempPath;
    }
}
