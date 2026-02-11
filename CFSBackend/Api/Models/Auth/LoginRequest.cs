using System.ComponentModel.DataAnnotations;

namespace ProjectName.Application.Models.Auth
{
    public record LoginRequest(
        [Required] string UserId,
        [Required] string Password
    );
}