import re

with open('Jellyfin.Plugin.SendToKindle/Services/KindleExtractionService.cs', 'r') as f:
    content = f.read()

content = re.sub(r'    public KindleExtractionService\(\s*ILibraryManager libraryManager,\s*ISmtpDeliveryService smtpService,\s*ILogger<KindleExtractionService> logger\)',
'''    /// <summary>
    /// Initializes a new instance of the <see cref="KindleExtractionService"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="smtpService">The smtp delivery service.</param>
    /// <param name="logger">The logger.</param>
    public KindleExtractionService(
        ILibraryManager libraryManager,
        ISmtpDeliveryService smtpService,
        ILogger<KindleExtractionService> logger)''', content, flags=re.MULTILINE)

with open('Jellyfin.Plugin.SendToKindle/Services/KindleExtractionService.cs', 'w') as f:
    f.write(content)
