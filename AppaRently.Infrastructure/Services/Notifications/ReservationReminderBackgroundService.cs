using AppaRently.Domain.Models;
using AppaRently.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppaRently.Infrastructure.Services.Notifications;

public sealed class ReservationReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationReminderBackgroundService> _logger;

    public ReservationReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reservation reminder worker failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppaRentlyDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<AppaRently.App.Interfaces.IEmailNotificationService>();
        var appNotificationService = scope.ServiceProvider.GetRequiredService<AppaRently.App.Interfaces.IAppNotificationService>();

        var now = DateTime.UtcNow;
        var windowEnd = now.AddHours(24);

        var reservations = await dbContext.Reservations
            .IgnoreQueryFilters()
            .Include(x => x.User)
            .Include(x => x.Apartment)
            .Where(x =>
                x.DeletedAt == null &&
                x.User != null &&
                x.Apartment != null &&
                (
                    (x.ReminderBeforeStartSentAt == null && x.CheckIn > now && x.CheckIn <= windowEnd) ||
                    (x.ReminderBeforeEndSentAt == null && x.CheckOut > now && x.CheckOut <= windowEnd)
                ))
            .ToListAsync(cancellationToken);

        foreach (var reservation in reservations)
        {
            if (reservation.User is null || reservation.Apartment is null || string.IsNullOrWhiteSpace(reservation.User.Email))
            {
                continue;
            }

            if (reservation.ReminderBeforeStartSentAt is null && reservation.CheckIn > now && reservation.CheckIn <= windowEnd)
            {
                var sent = await emailService.SendAsync(
                    reservation.User.Email!,
                    "Reservation reminder",
                    BuildStartReminderBody(reservation),
                    cancellationToken: cancellationToken);

                if (sent)
                {
                    reservation.ReminderBeforeStartSentAt = DateTime.UtcNow;
                    await appNotificationService.CreateAsync(
                        reservation.UserId,
                        "Reservation starts soon",
                        BuildStartReminderBody(reservation),
                        "reservation.reminder.start",
                        reservation.ApartmentId,
                        $"/Reservation/Show/{reservation.Id}",
                        cancellationToken);
                }
            }

            if (reservation.ReminderBeforeEndSentAt is null && reservation.CheckOut > now && reservation.CheckOut <= windowEnd)
            {
                var sent = await emailService.SendAsync(
                    reservation.User.Email!,
                    "Reservation ending reminder",
                    BuildEndReminderBody(reservation),
                    cancellationToken: cancellationToken);

                if (sent)
                {
                    reservation.ReminderBeforeEndSentAt = DateTime.UtcNow;
                    await appNotificationService.CreateAsync(
                        reservation.UserId,
                        "Reservation ends soon",
                        BuildEndReminderBody(reservation),
                        "reservation.reminder.end",
                        reservation.ApartmentId,
                        $"/Reservation/Show/{reservation.Id}",
                        cancellationToken);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string BuildStartReminderBody(Reservation reservation)
    {
        return
            $"Hello {reservation.User?.FullName},{Environment.NewLine}{Environment.NewLine}" +
            $"Your reservation at {reservation.Apartment?.Title} starts on {reservation.CheckIn:yyyy-MM-dd HH:mm}.{Environment.NewLine}" +
            $"Address: {reservation.Apartment?.Address}{Environment.NewLine}" +
            $"Check-out: {reservation.CheckOut:yyyy-MM-dd HH:mm}.{Environment.NewLine}{Environment.NewLine}" +
            "This is your 24-hour reminder before the reservation starts.";
    }

    private static string BuildEndReminderBody(Reservation reservation)
    {
        return
            $"Hello {reservation.User?.FullName},{Environment.NewLine}{Environment.NewLine}" +
            $"Your reservation at {reservation.Apartment?.Title} ends on {reservation.CheckOut:yyyy-MM-dd HH:mm}.{Environment.NewLine}" +
            $"Address: {reservation.Apartment?.Address}{Environment.NewLine}{Environment.NewLine}" +
            "This is your 24-hour reminder before the reservation ends.";
    }
}
