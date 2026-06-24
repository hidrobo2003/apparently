using AppaRently.App.DTOs.Users;
using AppaRently.App.Interfaces;
using AppaRently.Domain.Models;
using AppaRently.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AppaRently.Web.Client.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOptions<JwtOptions> _jwtOptions;

    public AccountController(
        IUserService userService,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IOptions<JwtOptions> jwtOptions)
    {
        _userService = userService;
        _signInManager = signInManager;
        _userManager = userManager;
        _jwtOptions = jwtOptions;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginRequest());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || user.DeletedAt is not null || !await _userManager.IsInRoleAsync(user, AppaRentlyRoles.Client))
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(request);
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(request);
        }

        await _signInManager.SignInAsync(user, request.RememberMe);
        DeleteJwtCookie();

        if (IsSafeReturnUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Apartment");
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new CreateUserRequest());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var response = await _userService.CreateClientAsync(request);
        if (!response.Success || response.Data is null)
        {
            ModelState.AddModelError(string.Empty, response.Message);
            return View(request);
        }

        return View("RegisterSuccess", response.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        DeleteJwtCookie();
        return RedirectToAction("Index", "Home");
    }

    private void DeleteJwtCookie()
    {
        Response.Cookies.Delete(_jwtOptions.Value.CookieName, new CookieOptions
        {
            Path = "/"
        });
    }

    private bool IsSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            return false;
        }

        var path = returnUrl.Split('?', '#')[0];
        return !path.EndsWith("/Storage", StringComparison.OrdinalIgnoreCase) &&
               !path.EndsWith("/Upgrade", StringComparison.OrdinalIgnoreCase) &&
               !path.EndsWith("/Destroy", StringComparison.OrdinalIgnoreCase);
    }
}
