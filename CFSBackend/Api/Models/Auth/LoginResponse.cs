namespace ProjectName.Application.Models.Auth
{
    public record LoginResponse(
        string Token,
        string UserId,
        string Department,
        string AccessMask,
        string DisplayName
    );
}