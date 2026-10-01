import re

def rep(filename, old, new):
    with open(filename, 'r') as f:
        content = f.read()
    with open(filename, 'w') as f:
        f.write(content.replace(old, new))

# IKindleExtractionService
with open('Jellyfin.Plugin.SendToKindle/Services/IKindleExtractionService.cs', 'r') as f:
    content = f.read()
# Re-do the comments for SendItemToKindleAsync
content = re.sub(r'/// <summary>.*?public Task SendItemToKindleAsync', 
'''/// <summary>
    /// Sends the item to kindle.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task SendItemToKindleAsync''', content, flags=re.DOTALL)
with open('Jellyfin.Plugin.SendToKindle/Services/IKindleExtractionService.cs', 'w') as f:
    f.write(content)

# PluginConfiguration
with open('Jellyfin.Plugin.SendToKindle/Configuration/PluginConfiguration.cs', 'r') as f:
    content = f.read()
content = re.sub(r'/// <summary>\s*/// (.*?)UserTargetKindleEmails.*?</summary>', 
'''/// <summary>
    /// Gets the user target kindle emails.
    /// </summary>''', content, flags=re.DOTALL)
with open('Jellyfin.Plugin.SendToKindle/Configuration/PluginConfiguration.cs', 'w') as f:
    f.write(content)

# KindleExtractionService
with open('Jellyfin.Plugin.SendToKindle/Services/KindleExtractionService.cs', 'r') as f:
    content = f.read()
content = re.sub(r'public KindleExtractionService\(ILibraryManager', 
'''/// <summary>
    /// Initializes a new instance of the <see cref="KindleExtractionService"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="smtpDeliveryService">The smtp delivery service.</param>
    /// <param name="logger">The logger.</param>
    public KindleExtractionService(ILibraryManager''', content)
with open('Jellyfin.Plugin.SendToKindle/Services/KindleExtractionService.cs', 'w') as f:
    f.write(content)

# SmtpDeliveryService
with open('Jellyfin.Plugin.SendToKindle/Services/SmtpDeliveryService.cs', 'r') as f:
    content = f.read()
content = re.sub(r'public SmtpDeliveryService\(ILogger', 
'''/// <summary>
    /// Initializes a new instance of the <see cref="SmtpDeliveryService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public SmtpDeliveryService(ILogger''', content)
with open('Jellyfin.Plugin.SendToKindle/Services/SmtpDeliveryService.cs', 'w') as f:
    f.write(content)
