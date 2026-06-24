using System.Security.Claims;
using AppaRently.App.DTOs.Users;
using AppaRently.App.Interfaces;
using AppaRently.Infrastructure.Data;
using AppaRently.Web.SuperAdmin.Models;
using AppaRently.Web.SuperAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AppaRently.Web.SuperAdmin.Controllers;

[Authorize(Roles = AppaRentlyRoles.SuperAdmin)]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly ISuperAdminPortalService _portalService;
    private readonly IOptions<JwtOptions> _jwtOptions;

    public UserController(
        IUserService userService,
        ISuperAdminPortalService portalService,
        IOptions<JwtOptions> jwtOptions)
    {
        _userService = userService;
        _portalService = portalService;
        _jwtOptions = jwtOptions;
    }

    public async Task<IActionResult> Index([FromQuery] SuperAdminUserDashboardQueryViewModel request)
    {
        var model = await _portalService.BuildUserDashboardAsync(request);
        return View(model);
    }

    public IActionResult Create()
    {
        return View(new CreateUserRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Storage(CreateUserRequest request)
    {
        var response = await _userService.CreateSuperAdminAsync(request);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message);
            return View("Create", request);
        }

        TempData["Message"] = response.Message;
        TempData["Success"] = response.Success;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Show(string id)
    {
        return await Detail(id);
    }

    public async Task<IActionResult> Detail(string id)
    {
        var model = await _portalService.BuildUserDetailAsync(id);
        if (model is null)
        {
            return RedirectToAction(nameof(Index));
        }

        return View("Detail", model);
    }

    public async Task<IActionResult> Profile()
    {
        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return RedirectToAction("Login", "Account");
        }

        var model = await _portalService.BuildProfileAsync(currentUserId);
        if (model is null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(model);
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
            return RedirectToAction(nameof(Profile));
        }

        return View(MapToEditViewModel(response.Data));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upgrade(string id, EditUserViewModel user)
    {
        if (!IsCurrentUser(id) || id != user.Id)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View("Edit", user);
        }

        var response = await _userService.EditUserAsync(id, new EditUserRequest
        {
            FullName = user.FullName,
            ChangePassword = user.ChangePassword,
            NewPassword = user.NewPassword,
            ConfirmNewPassword = user.ConfirmNewPassword
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
        var isCurrentUser = IsCurrentUser(id);
        if (!isCurrentUser && string.IsNullOrWhiteSpace(id))
        {
            return Forbid();
        }

        if (isCurrentUser)
        {
            var hasAnother = await _portalService.HasAnotherSuperAdminAsync(id);
            if (!hasAnother)
            {
                TempData["Message"] = "You cannot delete the last SuperAdmin account.";
                TempData["Success"] = false;
                return RedirectToAction(nameof(Profile));
            }
        }

        var response = await _userService.DeleteUserAsync(id);
        if (response.Success)
        {
            if (isCurrentUser)
            {
                Response.Cookies.Delete(_jwtOptions.Value.CookieName, new CookieOptions
                {
                    Path = "/"
                });

                return RedirectToAction("Login", "Account");
            }

            TempData["Message"] = response.Message;
            TempData["Success"] = true;
            return RedirectToAction(nameof(Index));
        }

        TempData["Message"] = response.Message;
        TempData["Success"] = response.Success;
        return isCurrentUser ? RedirectToAction(nameof(Profile)) : RedirectToAction(nameof(Index));
    }

    private string? GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private bool IsCurrentUser(string id) => string.Equals(GetCurrentUserId(), id, StringComparison.Ordinal);

    private static EditUserViewModel MapToEditViewModel(UserResponse user)
    {
        return new EditUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            DeletedAt = user.DeletedAt
        };
    }
}
