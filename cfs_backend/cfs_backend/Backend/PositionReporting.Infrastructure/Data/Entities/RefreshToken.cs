using System.ComponentModel.DataAnnotations;

namespace PositionReporting.Infrastructure.Data.Entities;

// Anemic Domain Model - pure data structure
public class RefreshToken : BaseEntity
{
    [Key]
    public Guid Id { get; set; }
    [MaxLength(256)] // For SHA256 hash in Base64
    public string TokenHash { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    [MaxLength(256)]
    public string? ReplacedByTokenHash { get; set; } // For token rotation tracking
}