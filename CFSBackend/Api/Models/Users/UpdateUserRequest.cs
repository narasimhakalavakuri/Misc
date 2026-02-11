using System.ComponentModel.DataAnnotations;

namespace ProjectName.Application.Models.Users
{
    public record UpdateUserRequest(
        string? Department,
        [Required] [MinLength(7)] string AccessMask
    );
}