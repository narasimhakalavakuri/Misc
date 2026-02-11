using ProjectName.Domain.Enums;

namespace ProjectName.Application.Models.PositionReports
{
    public record PositionReportListItemDto(
        string Dept,
        TransactionType Type,
        string Reference,
        string DrAcct,
        string DrAcctName,
        string DrCur,
        decimal DrAmount,
        string CrAcct,
        string CrAcctName,
        string CrCur,
        decimal CrAmount,
        string Calc,
        decimal Rate,
        DateTime? TransDate, // Can be null for incomplete
        DateTime ValueDate,
        DateTime IssueDate,
        string TheirRef,
        string MakerId,
        string? CheckerId,
        PositionReportStatus Status,
        string? Checkout, // Who has it checked out
        Guid Uid,
        string? PosRef,
        DateTime? CheckedDate
    );
}