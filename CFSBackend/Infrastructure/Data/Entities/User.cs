namespace ProjectName.Infrastructure.Data.Entities
{
    public class User
    {
        public Guid Id { get; set; } // Primary Key (uid in Delphi)
        public string UserId { get; set; } = string.Empty; // Domain qualified user ID
        public string PasswordHash { get; set; } = string.Empty; // Hashed password
        public string? Department { get; set; } // Assigned department (nullable)
        public string AccessMask { get; set; } = "0000000000000000000000000000"; // Access rights mask
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}