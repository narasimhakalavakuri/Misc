namespace ProjectName.Application.Models.PositionReports
{
    public record DuplicatePositionReportDto(
        string Currency,
        decimal Amount,
        string Reference,
        DateTime ValueDate,
        string AccountPair
    );
}