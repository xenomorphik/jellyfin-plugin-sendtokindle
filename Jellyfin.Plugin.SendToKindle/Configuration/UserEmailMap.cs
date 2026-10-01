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
