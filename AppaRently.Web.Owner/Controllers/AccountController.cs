using AppaRently.App.DTOs.Users;
using AppaRently.App.Interfaces;
using AppaRently.Domain.Models;
using AppaRently.Infrastructure.Data;
using AppaRently.Web.Owner.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;

namespace AppaRently.Web.Owner.Controllers;

public class AccountController : Controller
{
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOptions<JwtOptions> _jwtOptions;

    public AccountController(
        IEmailNotificationService emailNotificationService,
        IUserService userService,
        UserManager<ApplicationUser> userManager,
        IOptions<JwtOptions> jwtOptions)
    {
        _emailNotificationService = emailNotificationService;
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
        if (user is null || user.DeletedAt is not null || !await _userManager.IsInRoleAsync(user, AppaRentlyRoles.Owner))
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

        var response = await _userService.CreateOwnerAsync(request);
        if (!response.Success || response.Data is null)
        {
            ModelState.AddModelError(string.Empty, response.Message);
            return View(request);
        }

        return View("RegisterSuccess", response.Data);
    }

    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || user.DeletedAt is not null || !await _userManager.IsInRoleAsync(user, AppaRentlyRoles.Owner))
        {
            return View("ForgotPasswordSent");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = Url.Action(
            action: nameof(ResetPassword),
            controller: "Account",
            values: new
            {
                email = user.Email,
                token = WebUtility.UrlEncode(token)
            },
            protocol: Request.Scheme) ?? string.Empty;

        await _emailNotificationService.SendAsync(
            user.Email ?? string.Empty,
            "Reset your AppaRently password",
            BuildPasswordResetBody(user.FullName, resetLink));

        return View("ForgotPasswordSent");
    }

    [AllowAnonymous]
    public IActionResult ResetPassword(string email, string token)
    {
        return View(new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || user.DeletedAt is not null || !await _userManager.IsInRoleAsync(user, AppaRentlyRoles.Owner))
        {
            ModelState.AddModelError(string.Empty, "The password reset link is not valid.");
            return View(request);
        }

        var result = await _userManager.ResetPasswordAsync(user, WebUtility.UrlDecode(request.Token), request.Password);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, string.Join("; ", result.Errors.Select(x => x.Description)));
            return View(request);
        }

        TempData["Message"] = "Your password has been updated.";
        TempData["Success"] = true;
        return RedirectToAction(nameof(Login));
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

    private static string BuildPasswordResetBody(string fullName, string resetLink)
    {
        return
            $"Hello {fullName},{Environment.NewLine}{Environment.NewLine}" +
            "We received a request to reset your AppaRently password." + Environment.NewLine +
            $"Reset your password here: {resetLink}{Environment.NewLine}{Environment.NewLine}" +
            "If you did not request this, you can ignore this email.";
    }
}
