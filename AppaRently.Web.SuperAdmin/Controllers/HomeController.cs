using AppaRently.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace AppaRently.Web.SuperAdmin.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole(AppaRentlyRoles.SuperAdmin))
        {
            return RedirectToAction("Index", "User");
        }

        return RedirectToAction("Login", "Account");
    }
}
