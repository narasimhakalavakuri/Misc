using Microsoft.EntityFrameworkCore;
using PositionReporting.Infrastructure.Data;
using PositionReporting.Infrastructure.Data.Entities;
using PositionReporting.Infrastructure.Repositories.Interfaces;
using Microsoft.Kiota.Abstractions; // For Date
using Kiota.ApiClient.Models; // For PositionEntry_status
using PositionReporting.Api.Services.Implementations; // For IClock

namespace PositionReporting.Infrastructure.Repositories.Implementations;

public class PositionEntryRepository : IPositionEntryRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PositionEntryRepository> _logger;
    private readonly IClock _clock; // For getting current time consistently

    public PositionEntryRepository(ApplicationDbContext context, ILogger<PositionEntryRepository> logger, IClock clock)
    {
        _context = context;
        _logger = logger;
        _clock = clock;
    }

    public async Task<List<PositionEntry>> GetAllAsync(
        int? limit, int? offset, string? sort, string? filter,
        string? departmentId, Date? businessDate, List<string>? status)
    {
        _logger.LogDebug("Retrieving position entries with params: {DepartmentId}, {BusinessDate}, {Status}", departmentId, businessDate, status);
        var query = _context.PositionEntries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(departmentId))
        {
            query = query.Where(pe => pe.DepartmentId == departmentId);
        }

        if (businessDate.HasValue)
        {
            query = query.Where(pe => pe.ValueDate.Date == businessDate.Value.ToDateTime(TimeOnly.MinValue).Date);
        }

        if (status != null && status.Any())
        {
            query = query.Where(pe => status.Contains(pe.Status));
        }

        if (!string.IsNullOrWhiteSpace(filter))
        {
            // Example: filter by reference
            query = query.Where(pe => pe.Reference.Contains(filter) || pe.MakerId.Contains(filter));
        }

        if (!string.IsNullOrWhiteSpace(sort))
        {
            // Example: "valueDate:desc,referenceNumber:asc"
            var sortFields = sort.Split(',');
            foreach (var field in sortFields)
            {
                var parts = field.Trim().Split(':');
                if (parts.Length == 2)
                {
                    bool isDescending = parts[1].ToLowerInvariant() == "desc";
                    query = parts[0].ToLowerInvariant() switch
                    {
                        "valuedate" => isDescending ? query.OrderByDescending(pe => pe.ValueDate) : query.OrderBy(pe => pe.ValueDate),
                        "referencenumber" => isDescending ? query.OrderByDescending(pe => pe.ReferenceNumber) : query.OrderBy(pe => pe.ReferenceNumber),
                        "entrydate" => isDescending ? query.OrderByDescending(pe => pe.EntryDate) : query.OrderBy(pe => pe.EntryDate),
                        _ => query // Default or handle unsupported sort fields
                    };
                }
            }
        }
        else
        {
            query = query.OrderByDescending(pe => pe.ValueDate).ThenBy(pe => pe.ReferenceNumber); // Default sort
        }

        if (offset.HasValue)
        {
            query = query.Skip(offset.Value);
        }
        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<PositionEntry?> GetByIdAsync(string id)
    {
        _logger.LogDebug("Retrieving position entry by ID: {Id}", id);
        return await _context.PositionEntries.AsNoTracking().FirstOrDefaultAsync(pe => pe.Id == id);
    }

    public async Task AddAsync(PositionEntry entry)
    {
        _logger.LogDebug("Adding new position entry: {Id}", entry.Id);
        await _context.PositionEntries.AddAsync(entry);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PositionEntry entry)
    {
        _logger.LogDebug("Updating position entry: {Id}", entry.Id);
        _context.PositionEntries.Update(entry);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        _logger.LogDebug("Deleting position entry by ID: {Id}", id);
        var entry = await _context.PositionEntries.FindAsync(id);
        if (entry != null)
        {
            _context.PositionEntries.Remove(entry);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<PositionEntry>> GetAwaitingConfirmationEntriesAsync(
        int? limit, int? offset, string? sort, string? filter, string departmentId)
    {
        _logger.LogDebug("Retrieving awaiting confirmation entries for department: {DepartmentId}", departmentId);
        // Based on Delphi `sql_listing_incomplete`: `trans_date is null` AND `status not in ('K', 'E')` AND `checkout is null`
        var query = _context.PositionEntries.AsNoTracking()
            .Where(pe => pe.DepartmentId == departmentId &&
                         pe.EntryDate == null &&
                         pe.Status != PositionEntry_status.K.ToString() && // Not Cancelled
                         pe.Status != PositionEntry_status.E.ToString() && // Not Error/Correction
                         string.IsNullOrEmpty(pe.CheckoutId)); // Not checked out

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(pe => pe.Reference.Contains(filter));
        }

        // Apply sorting and pagination similar to GetAllAsync
        query = query.OrderBy(pe => pe.Status).ThenBy(pe => pe.ValueDate); // Example default sort

        if (offset.HasValue) query = query.Skip(offset.Value);
        if (limit.HasValue) query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<List<PositionEntry>> GetCorrectionCandidatesAsync(
        int? limit, int? offset, string? sort, string? filter, string departmentId)
    {
        _logger.LogDebug("Retrieving correction candidates for department: {DepartmentId}", departmentId);
        // Based on Delphi `sql_correction_listing`: `status = 'E'` AND `checkout is null`
        var query = _context.PositionEntries.AsNoTracking()
            .Where(pe => pe.DepartmentId == departmentId &&
                         pe.Status == PositionEntry_status.E.ToString() &&
                         string.IsNullOrEmpty(pe.CheckoutId));

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(pe => pe.Reference.Contains(filter));
        }

        // Apply sorting and pagination
        query = query.OrderBy(pe => pe.ValueDate); // Example default sort

        if (offset.HasValue) query = query.Skip(offset.Value);
        if (limit.HasValue) query = query.Take(limit.Value);

        return await query.ToListAsync();
    }

    public async Task<List<DuplicateCheckResult>> CheckDuplicatesAsync(Kiota.ApiClient.Models.DuplicateCheckRequest request)
    {
        _logger.LogDebug("Checking for duplicate entries based on request criteria.");
        // Mimic `sql_check_duplicate` logic from Delphi `positionsentry.ini`
        // select dr_cur as "CUR", dr_amount as "AMT", reference, value_date as "VALDATE", dr_acct +'-'+ cr_acct as "ACCT" from maintable where dept = '[edtDEPT]' and dr_acct = '[edtDRACCT]' and dr_cur = '[edtDRCUR]' and dr_amount = [edtDRAMT] and cr_acct = '[edtCRACCT]' and cr_cur = '[edtCRCUR]' and cr_amount = [edtCRAMT] and value_date > ([@GETDATE]-14) and uid <> '[@UID]'

        var query = _context.PositionEntries.AsNoTracking()
            .Where(pe => pe.DepartmentId == request.DepartmentId &&
                         pe.DebitCurrency == request.DebitCurrency &&
                         pe.DebitAmount == request.DebitAmount &&
                         pe.CreditCurrency == request.CreditCurrency &&
                         pe.CreditAmount == request.CreditAmount &&
                         pe.Reference == request.Reference &&
                         pe.ValueDate.Date == request.ValueDate!.Value.ToDateTime(TimeOnly.MinValue).Date &&
                         pe.ValueDate > _clock.UtcNow.AddDays(-14).Date);

        if (!string.IsNullOrWhiteSpace(request.DebitAccount))
        {
            query = query.Where(pe => pe.DebitAccount == request.DebitAccount);
        }
        if (!string.IsNullOrWhiteSpace(request.CreditAccount))
        {
            query = query.Where(pe => pe.CreditAccount == request.CreditAccount);
        }
        if (!string.IsNullOrWhiteSpace(request.ExcludePositionEntryId))
        {
            query = query.Where(pe => pe.Id != request.ExcludePositionEntryId);
        }

        return await query.Select(pe => new DuplicateCheckResult
        {
            Id = pe.Id,
            DebitCurrency = pe.DebitCurrency,
            DebitAmount = pe.DebitAmount,
            Reference = pe.Reference,
            ValueDate = pe.ValueDate,
            Accounts = (pe.DebitAccount ?? "") + "-" + (pe.CreditAccount ?? "")
        }).ToListAsync();
    }

    public async Task<IEnumerable<PositionEntry>> GetOutstandingEntriesForDepartmentAsync(string departmentCode, DateTime currentDate)
    {
        _logger.LogDebug("Retrieving outstanding entries for department: {DepartmentCode}", departmentCode);
        // Mimics Delphi SystemCloseCheck logic
        // INW transactions where trans_date is null and value_date < (currentDate + iDate1add)
        // Other transactions where trans_date is null and value_date <= (currentDate + iDate2add)
        // status not in ('U', 'K')
        var date1Add = -1; // Example from Delphi 'iDate1add'
        var date2Add = 0;  // Example from Delphi 'iDate2add'

        var inwConditionDate = currentDate.AddDays(date1Add);
        var otherConditionDate = currentDate.AddDays(date2Add);

        var query = _context.PositionEntries.AsNoTracking()
            .Where(pe => pe.DepartmentId == departmentCode &&
                         pe.EntryDate == null && // trans_date is null
                         pe.Status != PositionEntry_status.U.ToString() && // Not Uploaded (Approved)
                         pe.Status != PositionEntry_status.K.ToString() && // Not Cancelled
                         pe.Status != PositionEntry_status.X.ToString()); // Not logically deleted

        var inwEntries = query.Where(pe => pe.TransactionType == PositionEntry_transactionType.INW.ToString() && pe.ValueDate.Date < inwConditionDate.Date);
        var otherEntries = query.Where(pe => pe.TransactionType != PositionEntry_transactionType.INW.ToString() && pe.ValueDate.Date <= otherConditionDate.Date);

        return await inwEntries.Union(otherEntries).ToListAsync();
    }

    public async Task<bool> HasActiveEntriesForDepartmentAsync(string departmentCode)
    {
        _logger.LogDebug("Checking for active entries in department: {DepartmentCode}", departmentCode);
        return await _context.PositionEntries.AnyAsync(pe => pe.DepartmentId == departmentCode && pe.Status != PositionEntry_status.X.ToString());
    }

    public int GetNextReferenceSequence(string departmentId)
    {
        // This would typically involve a dedicated sequence table or a SQL function to get the next sequential number.
        // For demonstration, a simple placeholder.
        _logger.LogDebug("Generating next reference sequence for department: {DepartmentId}", departmentId);
        // In a real database, you'd use `SEQUENCE` objects or stored procedures to ensure atomicity and uniqueness.
        // For EF Core, it might involve a separate table with a counter, and locking mechanisms.
        // Example: a table `DepartmentSequence` with `DepartmentCode` and `LastSequenceNumber`.
        // Retrieve `LastSequenceNumber`, increment, update, and return. This needs to be transactional.
        // For simplicity, returning a fixed value or random for POC.
        return 123456; // Placeholder
    }
}