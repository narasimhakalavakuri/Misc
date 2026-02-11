namespace ProjectName.Application.Models.Reports
{
    public record ReportDefinitionDto(
        string Name,
        string Description,
        string FileName // For internal use
    );
}