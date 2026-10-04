using System.Collections.Generic;

namespace Jellyfin.Plugin.SendToKindle.Services;

/// <summary>
/// Data transfer object for EPUB metadata.
/// </summary>
public class EpubMetadataDto
{
    /// <summary>Gets or sets the title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets the authors.</summary>
    public IList<string> Authors { get; } = new List<string>();

    /// <summary>Gets the tags and genres.</summary>
    public IList<string> Subjects { get; } = new List<string>();

    /// <summary>Gets or sets the series name.</summary>
    public string? Series { get; set; }

    /// <summary>Gets or sets the cover image path.</summary>
    public string? CoverImagePath { get; set; }
}
