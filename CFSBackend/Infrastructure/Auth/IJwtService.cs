using System.Security.Claims;

namespace ProjectName.Infrastructure.Auth
{
    public interface IJwtService
    {
        string GenerateToken(Guid id, string userId, string department, string accessMask);
        ClaimsPrincipal? GetPrincipalFromToken(string token);
    }
}