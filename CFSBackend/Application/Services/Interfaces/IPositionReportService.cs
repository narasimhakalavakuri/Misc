using ProjectName.Application.Models.PositionReports;
using ProjectName.Domain.Enums;

namespace ProjectName.Application.Services.Interfaces
{
    public interface IPositionReportService
    {
        Task<PositionReportDto> CreatePositionReportAsync(CreatePositionReportRequest request, string currentDeptCode, string currentUserId);
        Task<PositionReportDto> UpdatePositionReportAsync(Guid uid, UpdatePositionReportRequest request, string currentDeptCode, string currentUserId);
        Task<PositionReportDto?> GetPositionReportByIdAsync(Guid uid, string currentDeptCode, string currentUserId);
        Task<IEnumerable<PositionReportListItemDto>> GetPositionReportsForListingAsync(PositionReportListFilter filter, string currentDeptCode, string currentUserId);
        Task<bool> UpdatePositionReportStatusAsync(UpdatePositionReportStatusRequest request, string currentDeptCode, string currentUserId);
        Task CheckoutPositionReportsAsync(Guid[] uids, string currentDeptCode, string currentUserId);
        Task CheckinPositionReportsAsync(Guid[] uids, string currentDeptCode, string currentUserId);
        Task LogicallyDeletePositionReportAsync(Guid uid, string currentDeptCode, string currentUserId);
        Task<IEnumerable<DuplicatePositionReportDto>> CheckForDuplicatesAsync(DuplicateCheckRequest request, string currentDeptCode, string currentUserId);
        Task<IEnumerable<string>> GetNostroAccountsAsync(string currencyCode);
        Task<CurrencyDetailsDto?> GetCurrencyDetailsAsync(string currencyCode);
        Task<string?> GetCurrencyForAccountAsync(string accountNumber);
    }
}