using ProjectName.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProjectName.Application.Models.PositionReports
{
    public record DuplicateCheckRequest(
        string Dept,
        string DrAcct,
        string DrCur,
        decimal DrAmount,
        string CrAcct,
        string CrCur,
        decimal CrAmount,
        DateTime ValueDate,
        Guid? Uid // Exclude self if updating
    );
}