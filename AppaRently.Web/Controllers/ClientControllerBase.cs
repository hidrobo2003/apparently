using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace AppaRently.Web.Client.Controllers;

public abstract class ClientControllerBase : Controller
{
    protected string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    protected bool IsCurrentUser(string id) => string.Equals(GetCurrentUserId(), id, StringComparison.Ordinal);

    protected IActionResult RedirectToLogin()
    {
        var returnUrl = $"{Request.Path}{Request.QueryString}";
        return RedirectToAction("Login", "Account", new { returnUrl });
    }
}
