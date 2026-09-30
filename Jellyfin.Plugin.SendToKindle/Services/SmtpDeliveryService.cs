using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SendToKindle.Services
{
    /// <summary>
    /// Implementation of the SMTP delivery service using standard .NET SmtpClient.
    /// </summary>
    public class SmtpDeliveryService : ISmtpDeliveryService
    {
        private readonly ILogger<SmtpDeliveryService> _logger;

        public SmtpDeliveryService(ILogger<SmtpDeliveryService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<bool> SendBookAsync(string bookFilePath, string bookTitle, CancellationToken cancellationToken)
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
                string.IsNullOrWhiteSpace(config.TargetKindleEmail))
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
                _logger.LogInformation("Preparing to send book '{Title}' to {Email}", bookTitle, config.TargetKindleEmail);

                using var message = new MailMessage();
                message.From = new MailAddress(config.SmtpUsername);
                message.To.Add(new MailAddress(config.TargetKindleEmail));
                message.Subject = "Send to Kindle";
                message.Body = $"Sending book: {bookTitle}";

                // Attach the book file
                using var attachment = new Attachment(bookFilePath);
                message.Attachments.Add(attachment);

                using var client = new SmtpClient(config.SmtpServer, config.SmtpPort);
                client.Credentials = new NetworkCredential(config.SmtpUsername, config.SmtpPassword);
                client.EnableSsl = true;

                // SendMailAsync does not natively accept a cancellation token in older frameworks, 
                // but registering cancellation can help abort the connection if needed.
                using (cancellationToken.Register(() => client.SendAsyncCancel()))
                {
                    await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
                }

                _logger.LogInformation("Successfully sent book '{Title}' to {Email}", bookTitle, config.TargetKindleEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending the book '{Title}' via SMTP.", bookTitle);
                return false;
            }
        }
    }
}
