using System.Diagnostics;
using AppaRently.Infrastructure.Data;
using AppaRently.Web.Owner.Models;
using AppaRently.Web.Owner.Services;
using Microsoft.AspNetCore.Mvc;

namespace AppaRently.Web.Owner.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole(AppaRentlyRoles.Owner))
        {
            return RedirectToAction("Index", "Apartment");
        }

        return RedirectToAction("Login", "Account");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
