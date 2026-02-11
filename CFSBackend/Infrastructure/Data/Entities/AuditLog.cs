namespace ProjectName.Infrastructure.Data.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string LogStr { get; set; } = string.Empty;
        public DateTime LogTime { get; set; }
    }
}