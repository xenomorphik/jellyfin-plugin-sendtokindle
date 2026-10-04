using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SendToKindle.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the target kindle email.
    /// </summary>
    public string TargetKindleEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the smtp server.
    /// </summary>
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the smtp port.
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Gets or sets the smtp username.
    /// </summary>
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the smtp password.
    /// </summary>
    public string SmtpPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a comma-separated list of Kindle Serial Numbers for DeDRM.
    /// </summary>
    public string KindleSerialNumbers { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user target kindle emails.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Needed for XML Serialization")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Needed for XML Serialization")]
    public UserEmailMap[] UserTargetKindleEmails { get; set; } = Array.Empty<UserEmailMap>();
}
