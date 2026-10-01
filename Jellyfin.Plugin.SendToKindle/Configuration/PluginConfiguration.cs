using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SendToKindle.Configuration
{
    /// <summary>
    /// Plugin configuration.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets the SMTP server address.
        /// </summary>
        public string SmtpServer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SMTP port.
        /// </summary>
        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// Gets or sets the SMTP username.
        /// </summary>
        public string SmtpUsername { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SMTP password.
        /// </summary>
        public string SmtpPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target Kindle email address (Legacy).
        /// </summary>
        public string TargetKindleEmail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the per-user target Kindle emails.
        /// </summary>
        public Dictionary<string, string> UserTargetKindleEmails { get; set; } = new Dictionary<string, string>();
    }
}
