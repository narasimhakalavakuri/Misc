using ProjectName.Domain.Enums;

namespace ProjectName.Application.Models.PositionReports
{
    public record PositionReportDto(
        Guid Uid,
        string Dept,
        TransactionType Type,
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
        DateTime TransDate,
        DateTime ValueDate,
        DateTime IssueDate,
        string TheirRef,
        string Reference,
        PositionReportStatus Status,
        string MakerId,
        string? CheckerId,
        string? PosRef,
        string? Checkout,
        DateTime? CorrectionDate,
        string? CorrectionId,
        DateTime? CheckedDate // Corresponds to edtAPPROVEDATE
    );
}