using System.ComponentModel.DataAnnotations;

namespace ProjectName.Application.Models.Users
{
    public record CreateUserRequest(
        [Required] [MaxLength(100)] string UserId,
        [Required] [MinLength(8)] string Password, // Minimum length based on common security practices
        string? Department,
        [Required] [MinLength(7)] string AccessMask // Based on ACC_ constants
    );
}