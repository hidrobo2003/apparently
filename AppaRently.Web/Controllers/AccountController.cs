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
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOptions<JwtOptions> _jwtOptions;

    public AccountController(
        IJwtTokenService jwtTokenService,
        IUserService userService,
        UserManager<ApplicationUser> userManager,
        IOptions<JwtOptions> jwtOptions)
    {
        _jwtTokenService = jwtTokenService;
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

        var roles = await _userManager.GetRolesAsync(user);
        var tokenResponse = await _jwtTokenService.CreateTokenAsync(
            new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                DeletedAt = user.DeletedAt
            },
            roles,
            request.RememberMe);
        AppendJwtCookie(tokenResponse.AccessToken, tokenResponse.ExpiresAt);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
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
        Response.Cookies.Delete(_jwtOptions.Value.CookieName, new CookieOptions
        {
            Path = "/"
        });
        return RedirectToAction("Index", "Home");
    }

    private void AppendJwtCookie(string token, DateTime expiresAt)
    {
        Response.Cookies.Append(_jwtOptions.Value.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = expiresAt,
            Path = "/"
        });
    }
}
