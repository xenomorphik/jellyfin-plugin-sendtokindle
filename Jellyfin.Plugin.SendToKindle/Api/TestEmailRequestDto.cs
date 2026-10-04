using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Plugin.SendToKindle.Api;

/// <summary>
/// Data transfer object for testing SMTP emails.
/// </summary>
public class TestEmailRequestDto
{
    /// <summary>
    /// Gets or sets the SMTP Server.
    /// </summary>
    [Required]
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP Port.
    /// </summary>
    [Required]
    public int SmtpPort { get; set; }

    /// <summary>
    /// Gets or sets the SMTP Username.
    /// </summary>
    [Required]
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP Password.
    /// </summary>
    [Required]
    public string SmtpPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target test email address.
    /// </summary>
    [Required]
    public string TargetEmail { get; set; } = string.Empty;
}
