using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SendToKindle.Configuration;

/// <summary>
/// A mapping between a user ID and a target kindle email.
/// </summary>
public class UserEmailMap
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

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
    /// Gets or sets the user target kindle emails.
    /// </summary>
    public List<UserEmailMap> UserTargetKindleEmails { get; set; } = new List<UserEmailMap>();
}
