using System;

namespace Jellyfin.Plugin.SendToKindle.Api;

/// <summary>
/// The user email dto.
/// </summary>
public class UserEmailDto
{
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
