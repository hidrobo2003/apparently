using AppaRently.App.DTOs.Users;
using AppaRently.App.Interfaces;
using AppaRently.Domain.Models;
using AppaRently.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AppaRently.Web.Client.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(
        IJwtTokenService jwtTokenService,
        IUserService userService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _jwtTokenService = jwtTokenService;
        _userService = userService;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<JwtTokenResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || user.DeletedAt is not null || !await HasOnlyRoleAsync(user, AppaRentlyRoles.Client))
        {
            return Unauthorized(new { message = "Invalid login attempt." });
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Invalid login attempt." });
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

        await _signInManager.SignInAsync(user, request.RememberMe);

        return Ok(tokenResponse);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<JwtTokenResponse>> Register([FromBody] CreateUserRequest request)
    {
        var response = await _userService.CreateClientAsync(request);
        if (!response.Success || response.Data is null)
        {
            return BadRequest(new { message = response.Message });
        }

        var tokenResponse = await _jwtTokenService.CreateTokenAsync(
            response.Data,
            new[] { response.Data.Role });

        var createdUser = await _userManager.FindByEmailAsync(response.Data.Email);
        if (createdUser is not null)
        {
            await _signInManager.SignInAsync(createdUser, isPersistent: true);
        }

        return Ok(tokenResponse);
    }

    private async Task<bool> HasOnlyRoleAsync(ApplicationUser user, string expectedRole)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.Count == 1 &&
               string.Equals(roles[0], expectedRole, StringComparison.Ordinal);
    }
}
