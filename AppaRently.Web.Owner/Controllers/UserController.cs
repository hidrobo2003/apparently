using System.Security.Claims;
using AppaRently.App.DTOs.Users;
using AppaRently.App.Interfaces;
using AppaRently.Infrastructure.Data;
using AppaRently.Web.Owner.Models;
using AppaRently.Web.Owner.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AppaRently.Web.Owner.Controllers;

[Authorize(Roles = AppaRentlyRoles.Owner)]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly IOwnerPortalService _ownerPortalService;
    private readonly IOptions<JwtOptions> _jwtOptions;

    public UserController(
        IUserService userService,
        IOwnerPortalService ownerPortalService,
        IOptions<JwtOptions> jwtOptions)
    {
        _userService = userService;
        _ownerPortalService = ownerPortalService;
        _jwtOptions = jwtOptions;
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Profile));
    }

    public async Task<IActionResult> Profile()
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return RedirectToAction("Login", "Account");
        }

        var model = await _ownerPortalService.BuildProfileAsync(currentUserId);
        if (model is null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(model);
    }

    public async Task<IActionResult> Show(string id)
    {
        if (!IsCurrentUser(id))
        {
            return Forbid();
        }

        return await Profile();
    }

    public async Task<IActionResult> Edit(string id)
    {
        if (!IsCurrentUser(id))
        {
            return Forbid();
        }

        var response = await _userService.GetByIdAsync(id);
        if (!response.Success || response.Data is null)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(response.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upgrade(string id, UserResponse user)
    {
        if (id != user.Id || !IsCurrentUser(id))
        {
            return Forbid();
        }

        var response = await _userService.EditUserAsync(id, new EditUserRequest
        {
            FullName = user.FullName
        });

        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message);
            return View("Edit", user);
        }

        TempData["Message"] = response.Message;
        TempData["Success"] = response.Success;
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Destroy(string id)
    {
        if (!IsCurrentUser(id))
        {
            return Forbid();
        }

        var response = await _userService.DeleteUserAsync(id);
        if (response.Success)
        {
            Response.Cookies.Delete(_jwtOptions.Value.CookieName, new CookieOptions
            {
                Path = "/"
            });

            return RedirectToAction("Login", "Account");
        }

        TempData["Message"] = response.Message;
        TempData["Success"] = response.Success;
        return RedirectToAction(nameof(Profile));
    }

    private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private bool IsCurrentUser(string id) => string.Equals(GetCurrentUserId(), id, StringComparison.Ordinal);
}
