using ProjectName.Domain.Enums;

namespace ProjectName.Infrastructure.Data.Entities
{
    public class PositionReport
    {
        public Guid Uid { get; set; } // Primary Key
        public string Dept { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public string DrAcct { get; set; } = string.Empty;
        public string DrAcctName { get; set; } = string.Empty;
        public string DrCur { get; set; } = string.Empty;
        public decimal DrAmount { get; set; }
        public string CrAcct { get; set; } = string.Empty;
        public string CrAcctName { get; set; } = string.Empty;
        public string CrCur { get; set; } = string.Empty;
        public decimal CrAmount { get; set; }
        public string Calc { get; set; } = string.Empty; // e.g., '*' or '/'
        public decimal Rate { get; set; }
        public DateTime TransDate { get; set; } // edtENTRYDATE (Input Date)
        public DateTime ValueDate { get; set; } // edtVALUEDATE
        public DateTime IssueDate { get; set; } // Date/Time of creation
        public string? TheirRef { get; set; } // Remarks (edtTHEIRREF)
        public string Reference { get; set; } = string.Empty; // Our reference (edtREFERENCE)
        public PositionReportStatus Status { get; set; } // 'M', 'K', 'U', 'E', 'X'
        public string MakerId { get; set; } = string.Empty;
        public string? CheckerId { get; set; }
        public string? PosRef { get; set; } // System generated reference number (irefno)
        public string? Checkout { get; set; } // UserID.SessionID for checkout lock
        public string? CheckoutSessionId { get; set; } // Just the session part of checkout
        public DateTime? CorrectionDate { get; set; }
        public string? CorrectionId { get; set; }
        public DateTime? CheckedDate { get; set; } // When it was checked/approved (edtAPPROVEDATE)
    }
}