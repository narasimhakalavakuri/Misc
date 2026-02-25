using System.ComponentModel.DataAnnotations;

namespace PositionReporting.Infrastructure.Data.Entities;

// Anemic Domain Model - pure data structure
public class User : BaseEntity
{
    [Key]
    public Guid Id { get; set; } // Internal unique ID
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty; // Domain qualified user ID (e.g., DOMAIN\USER1)
    [MaxLength(256)]
    public string? Email { get; set; } // For SAML UPN/email mapping
    [MaxLength(10)]
    public string? DepartmentId { get; set; }
    [MaxLength(20)]
    public string? AccessMask { get; set; } // e.g., "1110010"
    [MaxLength(256)]
    public string? PasswordHash { get; set; } // For fallback local authentication if needed
    public bool IsActive { get; set; } = true;
}

// Base entity for common properties
public abstract class BaseEntity
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}