namespace AppaRently.Infrastructure.Data;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = "smtp.gmail.com";

    public int Port { get; init; } = 587;

    public bool EnableSsl { get; init; } = true;

    public string UserName { get; init; } = "lukguest@gmail.com";

    public string Password { get; init; } = string.Empty;

    public string FromEmail { get; init; } = "lukguest@gmail.com";

    public string FromName { get; init; } = "AppaRently";

    public bool EnableReminderWorker { get; init; }
}
