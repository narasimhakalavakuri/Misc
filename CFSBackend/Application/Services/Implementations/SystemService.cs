using AutoMapper;
using Microsoft.Extensions.Configuration;
using ProjectName.Application.Models.System;
using ProjectName.Application.Services.Interfaces;
using ProjectName.Domain.Constants;
using ProjectName.Infrastructure.Data.Entities;
using ProjectName.Infrastructure.Data.Repositories.Interfaces;
using ProjectName.Infrastructure.Exceptions;

namespace ProjectName.Application.Services.Implementations
{
    public class SystemService : ISystemService
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IPositionReportRepository _positionReportRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<SystemService> _logger;
        private readonly IConfiguration _configuration;

        private readonly int _date1Add;
        private readonly int _date2Add;

        public SystemService(IDepartmentRepository departmentRepository, IPositionReportRepository positionReportRepository, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<SystemService> logger, IConfiguration configuration)
        {
            _departmentRepository = departmentRepository;
            _positionReportRepository = positionReportRepository;
            _auditLogRepository = auditLogRepository;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;

            _date1Add = _configuration.GetValue<int>("AppSettings:Date1Add", -1); // From Delphi: -1 for INW
            _date2Add = _configuration.GetValue<int>("AppSettings:Date2Add", 0); // From Delphi: 0 for OUTW
        }

        public async Task<IEnumerable<DepartmentStatusDto>> GetAllDepartmentStatusesAsync()
        {
            _logger.LogInformation("Fetching all department statuses.");
            var departments = await _departmentRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<DepartmentStatusDto>>(departments);
        }

        public async Task<DepartmentStatusDto?> GetDepartmentCurrentStatusAsync(string deptCode)
        {
            _logger.LogInformation("Fetching current status for department: {DeptCode}", deptCode);
            var department = await _departmentRepository.GetByDeptCodeAsync(deptCode);
            return _mapper.Map<DepartmentStatusDto?>(department);
        }

        public async Task CloseDepartmentAsync(string deptCode, DateTime verifyDate, string currentUserId)
        {
            _logger.LogInformation("Attempting to close department {DeptCode} by {UserId} for business date {VerifyDate}.", deptCode, currentUserId, verifyDate.ToShortDateString());

            // Normalize DeptCode to uppercase
            var normalizedDeptCode = deptCode.ToUpper();

            var department = await _departmentRepository.GetByDeptCodeAsync(normalizedDeptCode);
            if (department == null)
            {
                throw new NotFoundException($"Department {normalizedDeptCode} not found.");
            }

            // Check if department is already closed for today
            if (department.IsClosedToday())
            {
                throw new ValidationException($"Department {normalizedDeptCode} is already CLOSED for today. Close time: {department.ClosedDate?.ToString("yyyy-MM-dd HH:mm:ss")}.");
            }

            // Verify date check (Delphi: lblCURRDATE.Caption <> edtCLOSEDATE.Text)
            if (verifyDate.Date != DateTime.Today.Date)
            {
                throw new ValidationException("The verify date must match today's date to close the system.");
            }

            // Check for outstanding items (SystemCloseCheck equivalent)
            var outstandingItems = await GetOutstandingPositionReportsAsync(normalizedDeptCode);
            if (outstandingItems.Any())
            {
                var outstandingMessage = string.Join("\n", outstandingItems.Select(item =>
                    $"{item.ValueDate:d MMM yyyy} {item.Type} {item.Currency} {item.Amount:N2} {item.AccountName} {item.Reference}"));
                throw new ValidationException($"Outstanding items found for department {normalizedDeptCode}. Please resolve them before closing:\n{outstandingMessage}");
            }

            department.ClosedDate = DateTime.UtcNow;
            department.ClosedBy = currentUserId;
            department.UpdatedAt = DateTime.UtcNow;

            await _departmentRepository.UpdateAsync(department);
            await _departmentRepository.SaveAsync();

            // Log closing event
            await _auditLogRepository.LogEntryAsync(AuditConstants.ActionCloseSystem, $"Department {normalizedDeptCode} closed by {currentUserId}.", currentUserId);

            _logger.LogInformation("Department {DeptCode} successfully CLOSED by {UserId}.", normalizedDeptCode, currentUserId);
        }

        public async Task OpenDepartmentAsync(string deptCode, string currentUserId)
        {
            _logger.LogInformation("Attempting to open department {DeptCode} by {UserId}.", deptCode, currentUserId);

            // Normalize DeptCode to uppercase
            var normalizedDeptCode = deptCode.ToUpper();

            var department = await _departmentRepository.GetByDeptCodeAsync(normalizedDeptCode);
            if (department == null)
            {
                throw new NotFoundException($"Department {normalizedDeptCode} not found.");
            }

            if (!department.IsClosedToday())
            {
                _logger.LogInformation("Department {DeptCode} is already OPEN. No action required.", normalizedDeptCode);
                return;
            }

            department.ClosedDate = null; // Set to null to open
            department.ClosedBy = null;
            department.UpdatedAt = DateTime.UtcNow;

            await _departmentRepository.UpdateAsync(department);
            await _departmentRepository.SaveAsync();

            await _auditLogRepository.LogEntryAsync(AuditConstants.ActionOpenSystem, $"Department {normalizedDeptCode} opened by {currentUserId}.", currentUserId);

            _logger.LogInformation("Department {DeptCode} successfully OPENED by {UserId}.", normalizedDeptCode, currentUserId);
        }


        // Internal helper to get outstanding items, mimicking Delphi's SystemCloseCheck
        private async Task<List<OutstandingReportItem>> GetOutstandingPositionReportsAsync(string deptCode)
        {
            var outstandingItems = new List<OutstandingReportItem>();

            // For INW items: value_date < (today + date1Add) and status not in (U, K) and trans_date is null
            var inwCutoffDate = DateTime.Today.AddDays(_date1Add);
            var inwReports = await _positionReportRepository.GetOutstandingInwReportsAsync(deptCode, inwCutoffDate);
            foreach (var r in inwReports)
            {
                outstandingItems.Add(new OutstandingReportItem(
                    r.ValueDate, r.Type, r.DrCur, r.DrAmount, r.DrAcctName, r.Reference));
            }

            // For non-INW items: value_date <= (today + date2Add) and status not in (U, K) and trans_date is null
            var nonInwCutoffDate = DateTime.Today.AddDays(_date2Add);
            var nonInwReports = await _positionReportRepository.GetOutstandingNonInwReportsAsync(deptCode, nonInwCutoffDate);
            foreach (var r in nonInwReports)
            {
                // This logic might need refinement based on exact Delphi behavior for CR_CUR/CR_AMOUNT vs DR_CUR/DR_AMOUNT in display
                // For now, assuming it displays CR details for non-INW.
                outstandingItems.Add(new OutstandingReportItem(
                    r.ValueDate, r.Type, r.CrCur, r.CrAmount, r.CrAcctName, r.Reference));
            }

            return outstandingItems;
        }

        // Helper record for internal use in SystemService
        private record OutstandingReportItem(
            DateTime ValueDate,
            TransactionType Type,
            string Currency,
            decimal Amount,
            string AccountName,
            string Reference
        );
    }
}