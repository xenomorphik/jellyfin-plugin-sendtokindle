import os
import re

# Fix Plugin.cs
with open('Jellyfin.Plugin.SendToKindle/Plugin.cs', 'r') as f:
    plugin = f.read()
plugin = plugin.replace('using System.Collections.Generic;', 'using System.Collections.Generic;\nusing System.Globalization;')
plugin = plugin.replace('string.Format("{0}.Configuration.configPage.html", GetType().Namespace)', 'string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)')
with open('Jellyfin.Plugin.SendToKindle/Plugin.cs', 'w') as f:
    f.write(plugin)


# Fix PluginConfiguration.cs
with open('Jellyfin.Plugin.SendToKindle/Configuration/PluginConfiguration.cs', 'r') as f:
    config = f.read()
config = config.replace('public Dictionary<string, string> UserTargetKindleEmails { get; set; } = new Dictionary<string, string>();',
                        'public Dictionary<string, string> UserTargetKindleEmails { get; } = new Dictionary<string, string>();')
with open('Jellyfin.Plugin.SendToKindle/Configuration/PluginConfiguration.cs', 'w') as f:
    f.write(config)

# Fix SmtpDeliveryService.cs
with open('Jellyfin.Plugin.SendToKindle/Services/SmtpDeliveryService.cs', 'r') as f:
    smtp = f.read()
smtp = smtp.replace('/// <summary>\n    public SmtpDeliveryService(', '/// <summary>\n    /// Initializes a new instance of the <see cref="SmtpDeliveryService"/> class.\n    /// </summary>\n    /// <param name="logger">The logger.</param>\n    public SmtpDeliveryService(')
smtp = smtp.replace('message.Attachments.Add(attachmentPath);', 'await message.Attachments.AddAsync(attachmentPath, cancellationToken).ConfigureAwait(false);')
# Remove trailing whitespaces
smtp = '\n'.join([line.rstrip() for line in smtp.split('\n')])
with open('Jellyfin.Plugin.SendToKindle/Services/SmtpDeliveryService.cs', 'w') as f:
    f.write(smtp)

# Fix IKindleExtractionService.cs
with open('Jellyfin.Plugin.SendToKindle/Services/IKindleExtractionService.cs', 'r') as f:
    ikindle = f.read()
ikindle = ikindle.replace('/// <param name="cancellationToken">Cancellation token.</param>\n    /// <param name="userId">The user id.</param>', '/// <param name="userId">The user id.</param>\n    /// <param name="cancellationToken">Cancellation token.</param>')
with open('Jellyfin.Plugin.SendToKindle/Services/IKindleExtractionService.cs', 'w') as f:
    f.write(ikindle)

# Fix KindleExtractionService.cs
with open('Jellyfin.Plugin.SendToKindle/Services/KindleExtractionService.cs', 'r') as f:
    kindle = f.read()
kindle = kindle.replace('public KindleExtractionService(ILibraryManager libraryManager, ISmtpDeliveryService smtpDeliveryService, ILogger<KindleExtractionService> logger)', '/// <summary>\n    /// Initializes a new instance of the <see cref="KindleExtractionService"/> class.\n    /// </summary>\n    /// <param name="libraryManager">The library manager.</param>\n    /// <param name="smtpDeliveryService">The smtp delivery service.</param>\n    /// <param name="logger">The logger.</param>\n    public KindleExtractionService(ILibraryManager libraryManager, ISmtpDeliveryService smtpDeliveryService, ILogger<KindleExtractionService> logger)')
kindle = kindle.replace('}\n        public async Task', '}\n\n        public async Task')
# Remove trailing whitespaces
kindle = '\n'.join([line.rstrip() for line in kindle.split('\n')])
with open('Jellyfin.Plugin.SendToKindle/Services/KindleExtractionService.cs', 'w') as f:
    f.write(kindle)

# Fix WebInjectorService.cs
with open('Jellyfin.Plugin.SendToKindle/Services/WebInjectorService.cs', 'r') as f:
    web = f.read()
web = web.replace('public WebInjectorService(IApplicationPaths appPaths, ILogger<WebInjectorService> logger)', '/// <summary>\n    /// Initializes a new instance of the <see cref="WebInjectorService"/> class.\n    /// </summary>\n    /// <param name="appPaths">The application paths.</param>\n    /// <param name="logger">The logger.</param>\n    public WebInjectorService(IApplicationPaths appPaths, ILogger<WebInjectorService> logger)')
web = web.replace('public Task StartAsync(CancellationToken cancellationToken)', '/// <summary>\n    /// Starts the service.\n    /// </summary>\n    /// <param name="cancellationToken">The cancellation token.</param>\n    public async Task StartAsync(CancellationToken cancellationToken)')
web = web.replace('public Task StopAsync(CancellationToken cancellationToken)', '/// <summary>\n    /// Stops the service.\n    /// </summary>\n    /// <param name="cancellationToken">The cancellation token.</param>\n    public Task StopAsync(CancellationToken cancellationToken)')
web = web.replace('string content = File.ReadAllText(indexPath);', 'string content = await File.ReadAllTextAsync(indexPath, cancellationToken).ConfigureAwait(false);')
web = web.replace('content = content.Replace(match.Value, string.Empty);', 'content = content.Replace(match.Value, string.Empty, StringComparison.Ordinal);')
web = web.replace('content = content.Replace("</body>", $"    {newTag}\\n</body>");', 'content = content.Replace("</body>", $"    {newTag}\\n</body>", StringComparison.Ordinal);')
web = web.replace('File.WriteAllText(indexPath, content);', 'await File.WriteAllTextAsync(indexPath, content, cancellationToken).ConfigureAwait(false);')
web = web.replace('return Task.CompletedTask;', '')

# Fix empty catch blocks and method return type? Wait, if I made StartAsync async Task, I should remove `return Task.CompletedTask;` at the end.
web_lines = []
for line in web.split('\n'):
    if 'return Task.CompletedTask;' in line and 'StartAsync' in web:
        # We need to make sure we only remove it from StartAsync. We'll do it naively.
        pass
    else:
        pass
web = web.replace('            return Task.CompletedTask;\n        }\n\n        /// <summary>\n        /// Stops the service.', '        }\n\n        /// <summary>\n        /// Stops the service.')
# Remove trailing whitespaces
web = '\n'.join([line.rstrip() for line in web.split('\n')])
with open('Jellyfin.Plugin.SendToKindle/Services/WebInjectorService.cs', 'w') as f:
    f.write(web)


# Split UserEmailDto out of SendToKindleController.cs and document everything
with open('Jellyfin.Plugin.SendToKindle/Api/SendToKindleController.cs', 'r') as f:
    controller = f.read()

# remove UserEmailDto from controller
controller = re.sub(r'public class UserEmailDto\s*\{\s*public string Email \{ get; set; \}\s*\}', '', controller)
controller = controller.replace('public ActionResult<UserEmailDto> GetUserEmail()', '/// <summary>\n        /// Gets the user email.\n        /// </summary>\n        /// <returns>The user email.</returns>\n        [HttpGet("UserEmail")]\n        public ActionResult<UserEmailDto> GetUserEmail()')
controller = controller.replace('public ActionResult SetUserEmail([FromBody] UserEmailDto dto)', '/// <summary>\n        /// Sets the user email.\n        /// </summary>\n        /// <param name="dto">The dto.</param>\n        /// <returns>The result.</returns>\n        [HttpPost("UserEmail")]\n        public ActionResult SetUserEmail([FromBody] UserEmailDto dto)')

# There was another issue: SA1503 Braces should not be omitted.
controller = re.sub(r'if \((.*?)\)\n\s+return (.*?);', r'if (\1)\n            {\n                return \2;\n            }', controller)

with open('Jellyfin.Plugin.SendToKindle/Api/SendToKindleController.cs', 'w') as f:
    f.write(controller)

with open('Jellyfin.Plugin.SendToKindle/Api/UserEmailDto.cs', 'w') as f:
    f.write('''using System;

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
''')

