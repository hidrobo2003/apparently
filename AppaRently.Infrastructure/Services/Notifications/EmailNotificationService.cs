using System.Net.Mail;
using System.Text;
using AppaRently.App.Interfaces;
using AppaRently.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppaRently.Infrastructure.Services.Notifications;

public sealed class EmailNotificationService : IEmailNotificationService
{
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly SmtpOptions _options;

    public EmailNotificationService(
        IOptions<SmtpOptions> options,
        ILogger<EmailNotificationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendAsync(
        string toEmail,
        string subject,
        string body,
        IReadOnlyCollection<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            return false;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName, Encoding.UTF8),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            message.To.Add(toEmail.Trim());

            if (attachments is not null)
            {
                foreach (var attachment in attachments)
                {
                    var stream = new MemoryStream(attachment.Content, writable: false);
                    var mailAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);
                    message.Attachments.Add(mailAttachment);
                }
            }

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                Credentials = new System.Net.NetworkCredential(_options.UserName, _options.Password)
            };

            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP email could not be sent to {Recipient} with subject {Subject}", toEmail, subject);
            return false;
        }
    }
}
