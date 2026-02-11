using Microsoft.EntityFrameworkCore;
using ProjectName.Domain.Constants;
using ProjectName.Domain.Enums;
using ProjectName.Application.Models.PositionReports; // For filter DTO
using ProjectName.Infrastructure.Data.Entities;
using ProjectName.Infrastructure.Data.Repositories.Interfaces;

namespace ProjectName.Infrastructure.Data.Repositories
{
    public class PositionReportRepository : BaseRepository<PositionReport>, IPositionReportRepository
    {
        private readonly ApplicationDbContext _context;

        public PositionReportRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<PositionReport?> GetPositionReportForCorrectionAsync(Guid uid, string deptCode, string userId)
        {
            // Similar to sql_correction_entry, ensuring it's for current user's department and checked out by them or not at all
            var report = await _context.PositionReports
                .AsNoTracking()
                .Where(pr => pr.Uid == uid && pr.Dept == deptCode &&
                             (string.IsNullOrEmpty(pr.Checkout) || pr.Checkout.StartsWith($"{userId}.")))
                .FirstOrDefaultAsync();

            return report;
        }

        public async Task<IEnumerable<PositionReport>> GetFilteredPositionReportsAsync(PositionReportListFilter filter, string currentDeptCode, string currentUserId)
        {
            var query = _context.PositionReports.AsQueryable();

            // Apply department filter
            if (!string.IsNullOrEmpty(filter.DepartmentCode))
            {
                // If "All Sections" is used in Delphi, it typically means filtering by a broader department code (e.g., 'ABC' vs 'ABC-01')
                // For simplicity, if filter.DepartmentCode ends with '%', it's a "like" search, else exact match.
                // Assuming `filter.DepartmentCode` might come in as "ABC%" for all sections under "ABC".
                if (filter.DepartmentCode.EndsWith('%'))
                {
                    query = query.Where(pr => pr.Dept.StartsWith(filter.DepartmentCode.TrimEnd('%')));
                }
                else
                {
                    query = query.Where(pr => pr.Dept == filter.DepartmentCode);
                }
            }
            else
            {
                // Default to current user's department if no specific filter is provided
                query = query.Where(pr => pr.Dept == currentDeptCode);
            }

            // Apply business date filter (trans_date <= @BIZDATE from sql_listing)
            query = query.Where(pr => pr.TransDate <= filter.BusinessDate.ToUniversalTime());


            // Apply status filter (status in (@STATUS) from sql_listing)
            if (filter.Statuses?.Any() ?? false)
            {
                query = query.Where(pr => filter.Statuses.Contains(pr.Status));
            }
            else
            {
                // Default status filters for approval/correction/incomplete lists if no specific statuses are provided
                if (filter.IsCorrectionList)
                {
                    query = query.Where(pr => pr.Status == PositionReportStatus.Error && (string.IsNullOrEmpty(pr.Checkout) || pr.Checkout.StartsWith($"{currentUserId}.")));
                }
                else if (filter.IncludeIncomplete)
                {
                    query = query.Where(pr => pr.TransDate == default(DateTime) && pr.Status != PositionReportStatus.Cancelled && pr.Status != PositionReportStatus.Error && (string.IsNullOrEmpty(pr.Checkout) || pr.Checkout.StartsWith($"{currentUserId}.")));
                }
                else
                {
                    // Default for general listing/approval: show items with status Unchecked or Error
                    query = query.Where(pr => (pr.Status == PositionReportStatus.Unchecked || pr.Status == PositionReportStatus.Error));
                }
            }


            // Additional logic for sql_listing_incomplete: trans_date is null
            if (filter.IncludeIncomplete)
            {
                query = query.Where(pr => pr.TransDate == default(DateTime)); // Default(DateTime) for unassigned DateTime, which maps to NULL in DB
            }


            // Order by logic (from sql_listing)
            query = query.OrderBy(pr => pr.Status)
                         .ThenBy(pr => pr.Type)
                         .ThenBy(pr => pr.Reference); // Simplified sort key

            return await query.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<PositionReport>> GetManyByIdsAndDepartmentAsync(Guid[] uids, string deptCode)
        {
            return await _context.PositionReports
                .Where(pr => uids.Contains(pr.Uid) && pr.Dept == deptCode)
                .ToListAsync();
        }

        public async Task<IEnumerable<PositionReport>> FindDuplicatesAsync(
            string dept, string drAcct, string drCur, decimal drAmount,
            string crAcct, string crCur, decimal crAmount, DateTime valueDate,
            DateTime minDate, Guid? currentUid)
        {
            var query = _context.PositionReports
                .Where(pr => pr.Dept == dept &&
                             pr.DrAcct == drAcct &&
                             pr.DrCur == drCur &&
                             pr.DrAmount == drAmount &&
                             pr.CrAcct == crAcct &&
                             pr.CrCur == crCur &&
                             pr.CrAmount == crAmount &&
                             pr.ValueDate >= minDate);

            if (currentUid.HasValue)
            {
                query = query.Where(pr => pr.Uid != currentUid.Value);
            }

            return await query.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<string>> GetNostroAccountsByCurrencyAsync(string currencyCode)
        {
            return await _context.MiscSettings
                .AsNoTracking()
                .Where(ms => ms.DataId1 == "NOSTRO" && ms.DataId2 == currencyCode.ToUpper())
                .Select(ms => ms.Data01)
                .Where(d => d != null)
                .Select(d => d!)
                .ToListAsync();
        }

        public async Task<IEnumerable<PositionReport>> GetOutstandingInwReportsAsync(string deptCode, DateTime cutoffDate)
        {
            return await _context.PositionReports
                .AsNoTracking()
                .Where(pr => pr.Dept == deptCode &&
                             pr.TransDate == default(DateTime) && // trans_date is null
                             pr.ValueDate < cutoffDate.ToUniversalTime() &&
                             pr.Status != PositionReportStatus.Upload &&
                             pr.Status != PositionReportStatus.Cancelled &&
                             pr.Type == TransactionType.INW)
                .ToListAsync();
        }

        public async Task<IEnumerable<PositionReport>> GetOutstandingNonInwReportsAsync(string deptCode, DateTime cutoffDate)
        {
            return await _context.PositionReports
                .AsNoTracking()
                .Where(pr => pr.Dept == deptCode &&
                             pr.TransDate == default(DateTime) && // trans_date is null
                             pr.ValueDate <= cutoffDate.ToUniversalTime() &&
                             pr.Status != PositionReportStatus.Upload &&
                             pr.Status != PositionReportStatus.Cancelled &&
                             pr.Type != TransactionType.INW)
                .ToListAsync();
        }
    }
}