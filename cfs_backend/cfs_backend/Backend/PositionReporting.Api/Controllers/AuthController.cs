using System.Security.Claims;
using Kiota.ApiClient.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PositionReporting.Api.Models.Auth;
using PositionReporting.Api.Services.Interfaces;
using Sustainsys.Saml2.AspNetCore2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; // For IConfiguration

namespace PositionReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(ITokenService tokenService, ILogger<AuthController> logger, IConfiguration configuration)
    {
        _tokenService = tokenService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Initiates a SAML 2.0 Single Sign-On (SSO) request.
    /// This endpoint will redirect the user to the SAML Identity Provider (IdP) for authentication.
    /// </summary>
    [HttpGet("saml-login")]
    [AllowAnonymous]
    public async Task SamlLogin()
    {
        // This will trigger the SAML2 challenge and redirect to the IdP
        await HttpContext.ChallengeAsync(Saml2Defaults.AuthenticationScheme, new AuthenticationProperties
        {
            RedirectUri = _configuration["Saml2:ServiceProviderReturnUrl"]
        });
    }

    /// <summary>
    /// Handles the SAML Assertion Consumer Service (ACS) callback from the Identity Provider.
    /// After successful SAML authentication, this endpoint processes the assertion,
    /// validates the user, generates JWT and Refresh Tokens, and returns them to the frontend.
    /// The actual token generation and user validation is handled by Saml2PostAuthenticationHandler.
    /// This endpoint merely serves as the entry point for the SAML callback.
    /// </summary>
    /// <returns>A TokenResponse containing JWT and Refresh Token on success.</returns>
    [HttpGet("saml-acs")]
    [AllowAnonymous]
    public async Task<IActionResult> SamlAcsCallback()
    {
        // The Saml2PostAuthenticationHandler, configured in Program.cs,
        // will intercept the authentication result and generate tokens.
        // It will then place the TokenResponse in HttpContext.Items for retrieval here.

        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            _logger.LogError("SAML ACS authentication failed: {Error}", authenticateResult.Failure?.Message);
            return Unauthorized(new ErrorResponse { StatusCode = 401, Message = "SAML authentication failed.", Details = authenticateResult.Failure?.Message });
        }

        if (HttpContext.Items.TryGetValue("TokenResponse", out var tokenResponseObj) && tokenResponseObj is TokenResponse tokenResponse)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Clear SAML cookie after token generation
            return Ok(tokenResponse);
        }

        _logger.LogError("SAML ACS callback completed successfully, but no TokenResponse found in HttpContext.Items.");
        return Problem(detail: "Failed to generate security tokens after SAML authentication.", statusCode: 500, title: "Internal Server Error");
    }

    /// <summary>
    /// Refreshes an expired Access Token using a valid Refresh Token.
    /// </summary>
    /// <param name="request">The RefreshTokenRequest containing the refresh token.</param>
    /// <param name="correlationId">Optional unique identifier for the request.</param>
    /// <returns>A new TokenResponse with a fresh Access Token and a new Refresh Token.</returns>
    [HttpPost("refresh")]
    [AllowAnonymous] // Refresh tokens don't need a valid JWT
    [ProducesResponseType(typeof(TokenResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = "Refresh token is required.", Details = "The refresh token field cannot be empty." });
        }

        var result = await _tokenService.RefreshAccessTokenAsync(request.RefreshToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        // Handle specific token-related errors
        return result.Error.ErrorCode switch
        {
            TokenErrorCodes.InvalidToken => Unauthorized(new ErrorResponse { StatusCode = 401, Message = "Invalid refresh token.", Details = result.Error.Message }),
            TokenErrorCodes.ExpiredToken => Unauthorized(new ErrorResponse { StatusCode = 401, Message = "Refresh token expired.", Details = result.Error.Message }),
            TokenErrorCodes.TokenReplayed => Unauthorized(new ErrorResponse { StatusCode = 401, Message = "Refresh token replayed.", Details = result.Error.Message + " All associated tokens have been revoked." }),
            TokenErrorCodes.UserNotFound => Unauthorized(new ErrorResponse { StatusCode = 401, Message = "User not found.", Details = "Associated user for refresh token not found or is inactive." }),
            _ => Problem(detail: result.Error.Message, statusCode: 500, title: "Token Refresh Error")
        };
    }

    /// <summary>
    /// Revokes one or more refresh tokens for the current user.
    /// </summary>
    /// <param name="correlationId">Optional unique identifier for the request.</param>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Requires a valid JWT to logout
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> Logout([FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new ErrorResponse { StatusCode = 401, Message = "Invalid user identifier in token.", Details = "Could not identify the user from the current access token." });
        }

        await _tokenService.RevokeAllRefreshTokensForUserAsync(userId);
        _logger.LogInformation("User {UserId} logged out by revoking all refresh tokens.", userId);

        return NoContent(); // 204 No Content
    }

    /// <summary>
    /// Test endpoint to verify JWT authentication is working.
    /// </summary>
    [HttpGet("test-auth")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult TestAuth()
    {
        var userName = User.Identity?.Name ?? "Unknown";
        return Ok($"Authenticated as {userName} with JWT.");
    }
}

// DTO for refresh token request
public record RefreshTokenRequest(string RefreshToken);