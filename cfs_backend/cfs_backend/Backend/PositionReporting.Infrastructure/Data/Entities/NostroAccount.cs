using System.ComponentModel.DataAnnotations;

namespace PositionReporting.Infrastructure.Data.Entities;

// Anemic Domain Model - pure data structure
public class NostroAccount : BaseEntity
{
    // This entity represents an entry in TBL_MISC where dataid1 = 'NOSTRO' and dataid2 is the currency code.
    // data01 is the account number.
    [Key] // Composite key with CurrencyCode
    [MaxLength(50)]
    public string AccountNumber { get; set; } = string.Empty;
    [Key] // Composite key with AccountNumber
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    [MaxLength(50)]
    public string DataId1 { get; set; } = "NOSTRO"; // To filter for Nostro accounts
}