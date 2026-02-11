using ProjectName.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProjectName.Application.Models.PositionReports
{
    public record CreatePositionReportRequest(
        [Required] string Dept,
        [Required] TransactionType Type,
        [Required] string DrAcct,
        [Required] string DrAcctName,
        [Required] string DrCur,
        [Range(0.01, double.MaxValue)] decimal DrAmount,
        [Required] string CrAcct,
        [Required] string CrAcctName,
        [Required] string CrCur,
        [Range(0.01, double.MaxValue)] decimal CrAmount,
        [Required] string Calc,
        [Range(0.01, double.MaxValue)] decimal Rate,
        DateTime TransDate,
        DateTime ValueDate,
        DateTime IssueDate, // This will be set by the server
        [MaxLength(200)] string TheirRef, // Remarks
        [Required] [MaxLength(200)] string Reference // Our reference
    );
}