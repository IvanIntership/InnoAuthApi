using AuthApi.API.Dtos;
using AuthApi.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AuthApi.API.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    [HttpPost("register")]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Registers a new user",
        Description = "Creates a user in Keycloak, assigns a role, and saves the user entity locally.",
        OperationId = "RegisterUser"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "User was successfully registered", typeof(bool))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request body or parameters")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Role not found in Keycloak")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error or Keycloak integration failure")]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken ct = default)
    {
        var result = await _authService.RegisterUserAsync(request, null, ct);
        return Ok(result);
    }

    [HttpGet("login")]
    [SwaggerOperation(
        Summary = "Initiates user login",
        Description = "Redirects the user to the Keycloak authorization endpoint.",
        OperationId = "LoginUser"
    )]
    [SwaggerResponse(StatusCodes.Status302Found, "Redirects to Keycloak authorization URL")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error")]
    public IActionResult Login()
    {
        var url = _authService.GetAuthorizationRequestUrl();
        return Ok(url);
    }

    [HttpGet("exchange-code")]
    [SwaggerOperation(
        Summary = "Exchanges authorization code for tokens",
        Description = "Exchanges the temporary authorization code for JWT access and refresh tokens.",
        OperationId = "ExchangeCode"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Tokens retrieved successfully", typeof(TokenResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request body or parameters")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error or Keycloak integration failure")]
    public async Task<IActionResult> ExchangeCode([FromQuery(Name = "code")] string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Authorization code is required.");
        }

        var tokens = await _authService.ExchangeCodeForTokenAsync(code, ct);
        return Ok(tokens);
    }

    [HttpPost("logout")]
    [Authorize]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Logs out the user",
        Description = "Revokes the refresh token and ends the session in Keycloak.",
        OperationId = "LogoutUser"
    )]
    [SwaggerResponse(StatusCodes.Status204NoContent, "User was successfully logged out")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request body or parameters")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error or Keycloak integration failure")]
    public async Task<IActionResult> Logout([FromBody] LogOutUserRequest request, CancellationToken ct = default)
    {
        await _authService.SignOutUserAsync(request.Code, ct);
        return NoContent();
    }
    
    [HttpPost("refresh")]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Refreshes JWT access token",
        Description = "Exchanges a valid refresh token for a new pair of access and refresh tokens.",
        OperationId = "RefreshToken"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Tokens refreshed successfully", typeof(TokenResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid refresh token")]
    [SwaggerResponse(StatusCodes.Status500InternalServerError, "Internal service error or Keycloak integration failure")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct = default)
    {
        var tokens = await _authService.RefreshTokenAsync(request.RefreshToken, ct);
        return Ok(tokens);
    }
}