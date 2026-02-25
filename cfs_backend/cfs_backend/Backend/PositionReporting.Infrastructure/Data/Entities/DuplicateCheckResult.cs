using System.ComponentModel.DataAnnotations.Schema;

namespace PositionReporting.Infrastructure.Data.Entities;

// This entity is purely for mapping the result of a duplicate check query, not a persistent table.
[NotMapped]
public class DuplicateCheckResult
{
    public string Id { get; set; } = string.Empty;
    public string DebitCurrency { get; set; } = string.Empty;
    public float DebitAmount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime ValueDate { get; set; }
    public string Accounts { get; set; } = string.Empty; // Combined DR_ACCT-CR_ACCT
}