using AppaRently.App.ServiceResponse;

namespace AppaRently.App.Interfaces;

public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);

public interface IEmailNotificationService
{
    Task<bool> SendAsync(
        string toEmail,
        string subject,
        string body,
        IReadOnlyCollection<EmailAttachment>? attachments = null,
        CancellationToken cancellationToken = default);
}
