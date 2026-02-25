using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PositionReporting.Api.Models.Auth;
using Microsoft.Extensions.Configuration; // For IConfiguration

namespace PositionReporting.Api.Security;

public class JwtTokenGenerator
{
    private readonly IConfiguration _configuration;
    private readonly byte[] _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
        _key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured."));
        _issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured.");
        _audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured.");
        _accessTokenExpirationMinutes = _configuration.GetValue<int>("TokenManagement:AccessTokenExpirationMinutes", 15);
    }

    /// <summary>
    /// Generates a stateless JWT Access Token.
    /// </summary>
    /// <param name="claims">The claims to include in the token.</param>
    /// <returns>The generated JWT string.</returns>
    public TokenResponse GenerateAccessToken(IEnumerable<Claim> claims, Guid userId)
    {
        var securityKey = new SymmetricSecurityKey(_key);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Add standard claims if not present
        var identityClaims = new List<Claim>(claims);
        if (!identityClaims.Any(c => c.Type == JwtRegisteredClaimNames.Sub))
        {
            identityClaims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()));
        }
        if (!identityClaims.Any(c => c.Type == JwtRegisteredClaimNames.Jti))
        {
            identityClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        }
        if (!identityClaims.Any(c => c.Type == ClaimTypes.NameIdentifier))
        {
             identityClaims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        }

        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_accessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: identityClaims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        
        // This method only generates the access token. Refresh token is handled separately.
        // For the sake of simplicity, we return a partial TokenResponse here, 
        // the refresh token part will be filled by RefreshTokenManager.
        return new TokenResponse(accessToken, expires, "", DateTimeOffset.MinValue);
    }
}