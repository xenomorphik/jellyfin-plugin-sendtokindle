using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Jellyfin.Plugin.SendToKindle.Services;

/// <summary>
/// Implementation of the SMTP delivery service using MailKit.
/// </summary>
public class SmtpDeliveryService : ISmtpDeliveryService
{
    private readonly ILogger<SmtpDeliveryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmtpDeliveryService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public SmtpDeliveryService(ILogger<SmtpDeliveryService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SendBookAsync(string bookFilePath, string bookTitle, string targetEmail, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            _logger.LogError("Plugin configuration is null. Cannot send book.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.SmtpServer) ||
            string.IsNullOrWhiteSpace(config.SmtpUsername) ||
            string.IsNullOrWhiteSpace(config.SmtpPassword) ||
            string.IsNullOrWhiteSpace(targetEmail))
        {
            _logger.LogError("SMTP Configuration is incomplete. Please check the plugin settings.");
            return false;
        }

        if (!File.Exists(bookFilePath))
        {
            _logger.LogError("Book file not found at {Path}", bookFilePath);
            return false;
        }

        try
        {
            _logger.LogInformation("Preparing to send book '{Title}' to {Email}", bookTitle, targetEmail);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Jellyfin SendToKindle", config.SmtpUsername));
            message.To.Add(new MailboxAddress("Kindle Device", targetEmail));
            message.Subject = "Send to Kindle";

            var builder = new BodyBuilder
            {
                TextBody = $"Sending book: {bookTitle}"
            };

            // Attach the book file
            await builder.Attachments.AddAsync(bookFilePath, cancellationToken).ConfigureAwait(false);
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            // Connect to the SMTP server
            await client.ConnectAsync(config.SmtpServer, config.SmtpPort, SecureSocketOptions.Auto, cancellationToken).ConfigureAwait(false);

            // Authenticate
            await client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword, cancellationToken).ConfigureAwait(false);

            // Send the email
            await client.SendAsync(message, cancellationToken).ConfigureAwait(false);

            // Disconnect cleanly
            await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully sent book '{Title}' to {Email} using MailKit", bookTitle, targetEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending the book '{Title}' via SMTP.", bookTitle);
            return false;
        }
    }
}
