using System.Security.Claims;
using AppaRently.App.Interfaces;
using AppaRently.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppaRently.Web.SuperAdmin.Controllers;

[Authorize(Roles = AppaRentlyRoles.SuperAdmin)]
public class NotificationController : Controller
{
    private readonly IAppNotificationService _notificationService;

    public NotificationController(IAppNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index([FromQuery] bool unreadOnly = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var notifications = await _notificationService.GetInboxAsync(userId, unreadOnly);
        ViewData["UnreadOnly"] = unreadOnly;
        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Read(Guid id, [FromQuery] bool unreadOnly = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await _notificationService.MarkAsReadAsync(id, userId);
        }

        return RedirectToAction(nameof(Index), new { unreadOnly });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReadAll([FromQuery] bool unreadOnly = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await _notificationService.MarkAllAsReadAsync(userId);
        }

        return RedirectToAction(nameof(Index), new { unreadOnly });
    }
}
