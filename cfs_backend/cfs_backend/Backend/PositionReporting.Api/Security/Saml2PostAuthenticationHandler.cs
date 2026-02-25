using System.Security.Claims;
using PositionReporting.Api.Models.Auth;
using PositionReporting.Api.Services.Interfaces;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.WebSso;
using Microsoft.AspNetCore.Http; // For HttpContext
using Microsoft.Extensions.Logging; // For ILogger
using System.Threading.Tasks; // For Task

namespace PositionReporting.Api.Security;

public class Saml2PostAuthenticationHandler
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<Saml2PostAuthenticationHandler> _logger;

    public Saml2PostAuthenticationHandler(IUserService userService, ITokenService tokenService, ILogger<Saml2PostAuthenticationHandler> logger)
    {
        _userService = userService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Intercepts a successful SAML assertion, performs user validation,
    /// and generates JWT and Refresh Tokens.
    /// </summary>
    public async Task HandleAsync(AcsCommandResult commandResult, HttpContext httpContext)
    {
        _logger.LogInformation("SAML AcsCommandResultCreated notification received for user '{NameId}'.", commandResult.Principal.Identity?.Name);

        if (commandResult.Principal?.Identity is ClaimsIdentity claimsIdentity && claimsIdentity.IsAuthenticated)
        {
            // Extract claims from SAML assertion
            var samlClaims = claimsIdentity.Claims;

            // --- Tenant & User Validation ---
            // Example: Find a unique identifier for the user from SAML claims, e.g., UPN or email
            var userIdentifierClaim = samlClaims.FirstOrDefault(c => c.Type == ClaimTypes.Upn || c.Type == ClaimTypes.Email);
            if (userIdentifierClaim == null)
            {
                _logger.LogError("SAML assertion does not contain a suitable user identifier (UPN or Email).");
                // This will propagate as an authentication failure.
                commandResult.HandledResult = AuthenticateResult.Fail("SAML assertion missing user identifier.");
                return;
            }

            var externalUserId = userIdentifierClaim.Value;
            _logger.LogDebug("Attempting to validate user '{ExternalId}' from SAML.", externalUserId);

            var userValidationResult = await _userService.ValidateSamlUserAsync(externalUserId, samlClaims);

            if (!userValidationResult.IsSuccess)
            {
                _logger.LogWarning("SAML user validation failed for '{ExternalId}': {Error}", externalUserId, userValidationResult.Error);
                commandResult.HandledResult = AuthenticateResult.Fail($"User validation failed: {userValidationResult.Error}");
                return;
            }

            var validatedUser = userValidationResult.Value;
            _logger.LogInformation("User '{ExternalId}' successfully validated locally (Internal ID: {InternalId}).", externalUserId, validatedUser.Id);

            // --- Token Generation ---
            // The claims for the JWT should come from our local user store, not directly from SAML,
            // as SAML claims can be extensive and we only want specific ones in our JWT.
            var jwtClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, validatedUser.Id.ToString()),
                new Claim(ClaimTypes.Name, validatedUser.UserId), // Assuming UserId is like DOMAIN\USERNAME
                new Claim(ClaimTypes.Email, validatedUser.Email ?? ""),
                // Add specific application roles/permissions from our database
                new Claim("departmentId", validatedUser.DepartmentId ?? ""),
                new Claim("accessmask", validatedUser.AccessMask ?? "") // Assuming a combined access mask string
                // Map individual access mask flags to custom claim types or roles if needed
                // E.g., new Claim(ClaimTypes.Role, "posentry.query"), new Claim(ClaimTypes.Role, "posentry.input")
            };

            // Generate Access and Refresh Tokens
            var tokenGenerationResult = await _tokenService.GenerateTokensAsync(validatedUser.Id, jwtClaims);

            if (!tokenGenerationResult.IsSuccess)
            {
                _logger.LogError("Failed to generate JWT/Refresh tokens for user '{ExternalId}': {Error}", externalUserId, tokenGenerationResult.Error);
                commandResult.HandledResult = AuthenticateResult.Fail($"Failed to generate tokens: {tokenGenerationResult.Error}");
                return;
            }

            var tokenResponse = tokenGenerationResult.Value;

            // Store the TokenResponse in HttpContext.Items so the AuthController can retrieve and return it.
            httpContext.Items["TokenResponse"] = tokenResponse;

            _logger.LogInformation("Successfully generated JWT and Refresh Tokens for user '{ExternalId}'.", externalUserId);

            // Signal that the SAML authentication is handled and no further processing is needed for the original SAML flow
            commandResult.HandledResult = AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), Saml2Defaults.AuthenticationScheme));
            
            // To prevent redirection by the SAML middleware and allow our controller to return JSON,
            // we manually set the response status and clear the authentication cookie.
            // The SAML middleware expects a redirect to the client, but we want to return JSON directly.
            httpContext.Response.StatusCode = 200; // Indicate success to the client
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Clear SAML cookie

            // Prevent further processing by the SAML middleware. This is crucial to avoid redirect loops or unwanted redirects.
            httpContext.Response.CompleteAsync(); // Mark the response as complete
        }
        else
        {
            _logger.LogWarning("SAML AcsCommandResultCreated received with unauthenticated principal.");
            commandResult.HandledResult = AuthenticateResult.Fail("SAML principal not authenticated.");
        }
    }
}