using AppaRently.App.DTOs.Users;
using AppaRently.App.Interfaces;
using AppaRently.Domain.Models;
using AppaRently.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace AppaRently.Web.SuperAdmin.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOptions<JwtOptions> _jwtOptions;

    public AccountController(
        IUserService userService,
        UserManager<ApplicationUser> userManager,
        IOptions<JwtOptions> jwtOptions)
    {
        _userService = userService;
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
        if (user is null || user.DeletedAt is not null || !await _userManager.IsInRoleAsync(user, AppaRentlyRoles.SuperAdmin))
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(request);
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(request);
        }

        await SignInAsync(user, request.RememberMe);
        DeleteJwtCookie();

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "User");
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

        var response = await _userService.CreateSuperAdminAsync(request);
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
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        DeleteJwtCookie();

        return RedirectToAction(nameof(Login));
    }

    private async Task SignInAsync(ApplicationUser user, bool rememberMe)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("full_name", user.FullName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe
            });
    }

    private void DeleteJwtCookie()
    {
        Response.Cookies.Delete(_jwtOptions.Value.CookieName, new CookieOptions
        {
            Path = "/"
        });
    }
}
