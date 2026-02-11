namespace ProjectName.Application.Models.Users
{
    public record UserDto(
        Guid Id,
        string UserId,
        string? Department,
        string AccessMask,
        string DisplayName
    );
}