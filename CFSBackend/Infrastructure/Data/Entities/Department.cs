using ProjectName.Domain.Constants;

namespace ProjectName.Infrastructure.Data.Entities
{
    public class Department
    {
        public string DeptCode { get; set; } = string.Empty; // Primary Key
        public string DeptDesc { get; set; } = string.Empty;
        public string ApprType { get; set; } = "N"; // 'N' for normal, etc.
        public int RefNo { get; set; } // For sequence generation in Delphi (irefno)
        public string RefLock { get; set; } = "."; // Lock for reference number generation
        public DateTime? ClosedDate { get; set; } // Date/time department was closed for current business day
        public string? ClosedBy { get; set; } // User who closed the department
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Helper method to check if the department is closed for today
        public bool IsClosedToday()
        {
            return ClosedDate.HasValue && ClosedDate.Value.Date == DateTime.Today.Date;
        }

        // Helper method to check if the department is closed for a specific date (for future/past transactions)
        public bool IsClosedForDate(DateTime transactionDate)
        {
            // If the department was closed today, transactions for that date or earlier are not allowed.
            // If the department was closed on a past date, transactions for that past date or earlier are not allowed.
            return ClosedDate.HasValue && transactionDate.Date <= ClosedDate.Value.Date;
        }
    }
}