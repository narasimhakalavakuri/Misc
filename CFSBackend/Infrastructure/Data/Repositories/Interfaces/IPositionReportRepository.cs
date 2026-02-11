using ProjectName.Application.Models.PositionReports;
using ProjectName.Infrastructure.Data.Entities;

namespace ProjectName.Infrastructure.Data.Repositories.Interfaces
{
    public interface IPositionReportRepository : IBaseRepository<PositionReport>
    {
        Task<PositionReport?> GetPositionReportForCorrectionAsync(Guid uid, string deptCode, string userId);
        Task<IEnumerable<PositionReport>> GetFilteredPositionReportsAsync(PositionReportListFilter filter, string currentDeptCode, string currentUserId);
        Task<IEnumerable<PositionReport>> GetManyByIdsAndDepartmentAsync(Guid[] uids, string deptCode);
        Task<IEnumerable<PositionReport>> FindDuplicatesAsync(
            string dept, string drAcct, string drCur, decimal drAmount,
            string crAcct, string crCur, decimal crAmount, DateTime valueDate,
            DateTime minDate, Guid? currentUid);
        Task<IEnumerable<string>> GetNostroAccountsByCurrencyAsync(string currencyCode);
        Task<IEnumerable<PositionReport>> GetOutstandingInwReportsAsync(string deptCode, DateTime cutoffDate);
        Task<IEnumerable<PositionReport>> GetOutstandingNonInwReportsAsync(string deptCode, DateTime cutoffDate);
    }
}