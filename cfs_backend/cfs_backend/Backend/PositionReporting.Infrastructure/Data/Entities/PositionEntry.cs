using System.ComponentModel.DataAnnotations;

namespace PositionReporting.Infrastructure.Data.Entities;

// Anemic Domain Model - pure data structure
public class PositionEntry : BaseEntity
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = Guid.NewGuid().ToString(); // UID from legacy
    [MaxLength(10)]
    public string DepartmentId { get; set; } = string.Empty;
    [MaxLength(10)]
    public string TransactionType { get; set; } = string.Empty; // INW, OUTW, THRU, FE_EXCH
    [MaxLength(20)]
    public string? ReferenceNumber { get; set; } // POSREF from legacy
    public DateTimeOffset? EntryDate { get; set; } // TRANS_DATE from legacy
    public DateTime ValueDate { get; set; } // VALUE_DATE from legacy, OpenAPI uses Date, map to DateTime in EF
    public DateTimeOffset? IssueDate { get; set; } // ISSUE_DATE from legacy
    [MaxLength(200)]
    public string? TheirReference { get; set; } // THEIR_REF from legacy
    [MaxLength(200)]
    public string Reference { get; set; } = string.Empty; // REFERENCE from legacy
    [MaxLength(50)]
    public string? DebitAccount { get; set; } // DR_ACCT from legacy
    [MaxLength(100)]
    public string? DebitAccountName { get; set; } // DR_ACCTNAME from legacy
    [MaxLength(3)]
    public string DebitCurrency { get; set; } = string.Empty; // DR_CUR from legacy
    public float DebitAmount { get; set; } // DR_AMOUNT from legacy (float as per OpenAPI)
    [MaxLength(50)]
    public string? CreditAccount { get; set; } // CR_ACCT from legacy
    [MaxLength(100)]
    public string? CreditAccountName { get; set; } // CR_ACCTNAME from legacy
    [MaxLength(3)]
    public string CreditCurrency { get; set; } = string.Empty; // CR_CUR from legacy
    public float CreditAmount { get; set; } // CR_AMOUNT from legacy (float as per OpenAPI)
    [MaxLength(1)]
    public string CalculationSymbol { get; set; } = string.Empty; // CALC from legacy ('*', '/')
    public float ExchangeRate { get; set; } // RATE from legacy (float as per OpenAPI)
    [MaxLength(1)]
    public string Status { get; set; } = string.Empty; // M, E, K, U, X
    [MaxLength(100)]
    public string MakerId { get; set; } = string.Empty; // MAKER_ID from legacy
    [MaxLength(100)]
    public string? CheckerId { get; set; } // CHECKER_ID from legacy
    public DateTimeOffset? CorrectionDate { get; set; } // CORRECTION_DATE from legacy
    [MaxLength(150)]
    public string? CheckoutId { get; set; } // CHECKOUT from legacy (USERID.RANDOMSTRING)
    public DateTimeOffset? ApprovedDate { get; set; } // CHECKED_DATE from legacy
}