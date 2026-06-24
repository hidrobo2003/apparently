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

    public AuthController(
        IJwtTokenService jwtTokenService,
        IUserService userService,
        UserManager<ApplicationUser> userManager)
    {
        _jwtTokenService = jwtTokenService;
        _userService = userService;
        _userManager = userManager;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<JwtTokenResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || user.DeletedAt is not null || !await _userManager.IsInRoleAsync(user, AppaRentlyRoles.Client))
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

        return Ok(tokenResponse);
    }
}
