using System.ComponentModel.DataAnnotations;

namespace ProjectName.Application.Models.Users
{
    public record ChangePasswordRequest(
        [Required] Guid UserId, // ID of the user whose password is being changed
        [Required] string OldPassword,
        [Required] [MinLength(8)] string NewPassword,
        [Required] string ConfirmPassword
    );
}