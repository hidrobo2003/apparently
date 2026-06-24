using AppaRently.App.DTOs.Reservations;
using AppaRently.App.DTOs.Users;
using AppaRently.App.Interfaces;
using AppaRently.Infrastructure.Data;
using AppaRently.Web.Client.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AppaRently.Web.Client.Controllers;

public class UserController : ClientControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly IUserService _userService;

    public UserController(
        IReservationService reservationService,
        IUserService userService,
        IOptions<JwtOptions> jwtOptions)
    {
        _reservationService = reservationService;
        _userService = userService;
        _jwtOptions = jwtOptions;
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Profile));
    }

    public async Task<IActionResult> Profile()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToLogin();
        }

        return await RenderProfileAsync(userId);
    }

    public async Task<IActionResult> Show(string id)
    {
        if (!IsCurrentUser(id))
        {
            return RedirectToLogin();
        }

        return await RenderProfileAsync(id);
    }

    public async Task<IActionResult> Edit(string id)
    {
        if (!IsCurrentUser(id))
        {
            return RedirectToLogin();
        }

        var response = await _userService.GetByIdAsync(id);
        if (!response.Success || response.Data is null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(response.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upgrade(string id, UserResponse user)
    {
        if (id != user.Id || !IsCurrentUser(id))
        {
            return RedirectToLogin();
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
            return RedirectToLogin();
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

    private async Task<IActionResult> RenderProfileAsync(string userId)
    {
        var userResponse = await _userService.GetByIdAsync(userId);
        if (!userResponse.Success || userResponse.Data is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var reservationsResponse = await _reservationService.GetAllAsync(new ReservationSearchRequest
        {
            UserId = userId
        });

        var model = new ProfileViewModel
        {
            User = userResponse.Data,
            Reservations = reservationsResponse.Data?
                .OrderByDescending(x => x.CreatedAt)
                .ToList() ?? new List<ReservationResponse>()
        };

        return View("Profile", model);
    }

}
