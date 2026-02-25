using System.ComponentModel.DataAnnotations;

namespace PositionReporting.Infrastructure.Data.Entities;

// Anemic Domain Model - pure data structure
public class Currency : BaseEntity
{
    [Key]
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;
    public int DecimalPrecision { get; set; }
    public float ExchangeRateTTS { get; set; } // Using float as per OpenAPI spec
}