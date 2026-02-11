using AutoMapper;
using Microsoft.Extensions.Configuration;
using ProjectName.Application.Models.PositionReports;
using ProjectName.Application.Services.Interfaces;
using ProjectName.Domain.Constants;
using ProjectName.Domain.Enums;
using ProjectName.Infrastructure.Data.Entities;
using ProjectName.Infrastructure.Data.Repositories.Interfaces;
using ProjectName.Infrastructure.Exceptions;

namespace ProjectName.Application.Services.Implementations
{
    public class PositionReportService : IPositionReportService
    {
        private readonly IPositionReportRepository _positionReportRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly ICurrencyRepository _currencyRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PositionReportService> _logger;
        private readonly IConfiguration _configuration;

        private readonly string _homeCurrency;
        private readonly int _duplicateCheckDays;
        private readonly int _date1Add; // for INW outstanding check
        private readonly int _date2Add; // for OUTW outstanding check

        public PositionReportService(
            IPositionReportRepository positionReportRepository,
            IAuditLogRepository auditLogRepository,
            IDepartmentRepository departmentRepository,
            IUserRepository userRepository,
            ICustomerRepository customerRepository,
            ICurrencyRepository currencyRepository,
            IMapper mapper,
            ILogger<PositionReportService> logger,
            IConfiguration configuration)
        {
            _positionReportRepository = positionReportRepository;
            _auditLogRepository = auditLogRepository;
            _departmentRepository = departmentRepository;
            _userRepository = userRepository;
            _customerRepository = customerRepository;
            _currencyRepository = currencyRepository;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;

            _homeCurrency = _configuration.GetValue<string>("AppSettings:HomeCurrency") ?? "SGD";
            _duplicateCheckDays = _configuration.GetValue<int>("AppSettings:DuplicateCheckDays", 14);
            _date1Add = _configuration.GetValue<int>("AppSettings:Date1Add", -1); // From Delphi: -1 for INW
            _date2Add = _configuration.GetValue<int>("AppSettings:Date2Add", 0); // From Delphi: 0 for OUTW
        }

        public async Task<PositionReportDto> CreatePositionReportAsync(CreatePositionReportRequest request, string currentDeptCode, string currentUserId)
        {
            _logger.LogInformation("Creating position report by {UserId} in {DeptCode}.", currentUserId, currentDeptCode);

            // Basic validation and business logic from Delphi PosiRptEntryFrm.pas:VerifyData
            await ValidatePositionReport(request, currentDeptCode);

            var duplicateReports = await CheckForDuplicatesInternalAsync(
                new DuplicateCheckRequest(
                    request.Dept, request.DrAcct, request.DrCur, request.DrAmount,
                    request.CrAcct, request.CrCur, request.CrAmount, request.ValueDate, null
                ),
                currentDeptCode, currentUserId
            );
            if (duplicateReports.Any())
            {
                // In Delphi, this triggered a manual override dialog.
                // For API, we can either return these duplicates and let the UI handle a warning/override prompt,
                // or throw a specific exception that the API controller catches to return a 409 Conflict.
                // For now, let's throw an exception indicating duplicates are found.
                var duplicateMessages = duplicateReports.Select(d => $"{d.Currency} {d.Amount:N2} {d.Reference} {d.ValueDate:d MMM yyyy} {d.AccountPair}");
                throw new ValidationException($"Possible duplicate transaction(s) found: {string.Join("; ", duplicateMessages)}. Manual override required.");
            }

            var positionReport = _mapper.Map<PositionReport>(request);
            positionReport.MakerId = currentUserId;
            positionReport.Dept = currentDeptCode; // Ensure report is created under current user's department
            positionReport.IssueDate = DateTime.UtcNow; // Set on server
            positionReport.TransDate = request.TransDate.ToUniversalTime(); // Ensure UTC for consistency
            positionReport.ValueDate = request.ValueDate.ToUniversalTime(); // Ensure UTC for consistency

            // Generate PosRef (Reference Number) - Delphi's irefno logic
            var dept = await _departmentRepository.GetByDeptCodeAsync(currentDeptCode);
            if (dept == null)
            {
                throw new NotFoundException($"Department {currentDeptCode} not found for reference number generation.");
            }
            var nextRefNo = dept.RefNo + 1; // Assuming RefNo is an integer in DB
            dept.RefNo = nextRefNo;
            positionReport.PosRef = $"{currentDeptCode}{DateTime.Now.Year}{nextRefNo:D6}"; // D6 for 6 digits padding

            await _departmentRepository.UpdateAsync(dept); // Update department with new RefNo
            await _positionReportRepository.AddAsync(positionReport);
            await _positionReportRepository.SaveAsync();

            await _auditLogRepository.LogEntryAsync(AuditConstants.ActionCreatePositionReport, $"Position report {positionReport.Uid} created with reference {positionReport.PosRef}.", currentUserId);
            _logger.LogInformation("Position report {Uid} created by {UserId}.", positionReport.Uid, currentUserId);

            return _mapper.Map<PositionReportDto>(positionReport);
        }

        public async Task<PositionReportDto> UpdatePositionReportAsync(Guid uid, UpdatePositionReportRequest request, string currentDeptCode, string currentUserId)
        {
            _logger.LogInformation("Updating position report {Uid} by {UserId} in {DeptCode}.", uid, currentUserId, currentDeptCode);

            var existingReport = await _positionReportRepository.GetByIdAsync(uid);
            if (existingReport == null || existingReport.Dept != currentDeptCode)
            {
                _logger.LogWarning("Position report {Uid} not found or not in user's department {DeptCode}.", uid, currentDeptCode);
                throw new NotFoundException($"Position report with UID {uid} not found or you do not have access to it.");
            }

            // Basic validation and business logic from Delphi PosiRptEntryFrm.pas:VerifyData
            await ValidatePositionReport(request, currentDeptCode);

            var duplicateReports = await CheckForDuplicatesInternalAsync(
                new DuplicateCheckRequest(
                    request.Dept, request.DrAcct, request.DrCur, request.DrAmount,
                    request.CrAcct, request.CrCur, request.CrAmount, request.ValueDate, uid
                ),
                currentDeptCode, currentUserId
            );
            if (duplicateReports.Any())
            {
                var duplicateMessages = duplicateReports.Select(d => $"{d.Currency} {d.Amount:N2} {d.Reference} {d.ValueDate:d MMM yyyy} {d.AccountPair}");
                throw new ValidationException($"Possible duplicate transaction(s) found: {string.Join("; ", duplicateMessages)}. Manual override required.");
            }

            _mapper.Map(request, existingReport);
            existingReport.CorrectionDate = DateTime.UtcNow;
            existingReport.CorrectionId = currentUserId;
            existingReport.Status = PositionReportStatus.Unchecked; // Status set to 'M' (Unchecked) after correction
            existingReport.Checkout = null; // Clear checkout after correction

            await _positionReportRepository.UpdateAsync(existingReport);
            await _positionReportRepository.SaveAsync();

            await _auditLogRepository.LogEntryAsync(AuditConstants.ActionUpdatePositionReport, $"Position report {uid} corrected by {currentUserId}.", currentUserId);
            _logger.LogInformation("Position report {Uid} updated by {UserId}.", uid, currentUserId);

            return _mapper.Map<PositionReportDto>(existingReport);
        }

        public async Task<PositionReportDto?> GetPositionReportByIdAsync(Guid uid, string currentDeptCode, string currentUserId)
        {
            _logger.LogInformation("Fetching position report {Uid} by {UserId} in {DeptCode}.", uid, currentUserId, currentDeptCode);
            var report = await _positionReportRepository.GetPositionReportForCorrectionAsync(uid, currentDeptCode, currentUserId);
            return _mapper.Map<PositionReportDto?>(report);
        }

        public async Task<IEnumerable<PositionReportListItemDto>> GetPositionReportsForListingAsync(PositionReportListFilter filter, string currentDeptCode, string currentUserId)
        {
            _logger.LogInformation("Fetching position reports for listing for {DeptCode} with filter {@Filter} by {UserId}.", currentDeptCode, filter, currentUserId);

            var reports = await _positionReportRepository.GetFilteredPositionReportsAsync(filter, currentDeptCode, currentUserId);

            // Handle default status filtering if not provided and not a specific list type
            if (!filter.Statuses?.Any() ?? true)
            {
                if (filter.IsCorrectionList)
                {
                    filter = filter with { Statuses = new[] { PositionReportStatus.Error } }; // 'E' for Correction
                }
                else if (filter.IncludeIncomplete)
                {
                    filter = filter with { Statuses = new[] { PositionReportStatus.Unchecked } }; // 'M' for Incomplete (awaiting confirmation)
                }
                else
                {
                    filter = filter with { Statuses = new[] { PositionReportStatus.Unchecked, PositionReportStatus.Error } }; // Default for Approval list (M, E)
                }
            }


            return _mapper.Map<IEnumerable<PositionReportListItemDto>>(reports);
        }

        public async Task<bool> UpdatePositionReportStatusAsync(UpdatePositionReportStatusRequest request, string currentDeptCode, string currentUserId)
        {
            _logger.LogInformation("Updating status of {Count} position reports to {Status} by {UserId} in {DeptCode}.", request.Uids.Length, request.Status, currentUserId, currentDeptCode);

            var serverTime = DateTime.UtcNow;

            // Check if department is closed before allowing approval/rejection ('U' or 'K')
            var dept = await _departmentRepository.GetByDeptCodeAsync(currentDeptCode);
            if (dept == null)
            {
                throw new NotFoundException($"Department {currentDeptCode} not found.");
            }
            if (dept.IsClosedToday() && (request.Status == PositionReportStatus.Upload || request.Status == PositionReportStatus.Cancelled))
            {
                throw new ValidationException($"The system is closed for department {currentDeptCode}. No operations allowed except viewing.");
            }

            var reportsToUpdate = await _positionReportRepository.GetManyByIdsAndDepartmentAsync(request.Uids, currentDeptCode);

            if (reportsToUpdate == null || !reportsToUpdate.Any())
            {
                throw new NotFoundException("One or more position reports not found or not in your department.");
            }

            bool allSucceeded = true;
            foreach (var report in reportsToUpdate)
            {
                // Only allow update if the report is checked out by the current user OR not checked out at all
                if (!string.IsNullOrEmpty(report.Checkout) && report.Checkout != $"{currentUserId}.{report.CheckoutSessionId}")
                {
                    _logger.LogWarning("Position report {Uid} is checked out by another user, cannot update status.", report.Uid);
                    allSucceeded = false;
                    continue; // Skip this one, but continue with others
                }

                // Logical deletion (status 'X') as per Delphi 1.4.0 change
                if (request.Status == PositionReportStatus.Deleted)
                {
                    report.Status = PositionReportStatus.Deleted;
                    report.Dept = AppConstants.LogicalDeletedDeptPrefix + report.Dept; // Prepend char(1) to dept as per Delphi
                    report.Checkout = null; // Clear checkout
                    report.CheckerId = currentUserId;
                    report.CheckedDate = serverTime;
                    await _auditLogRepository.LogEntryAsync(AuditConstants.ActionDeletePositionReport, $"Position report {report.Uid} logically deleted by {currentUserId}.", currentUserId);
                }
                else
                {
                    // For 'U', 'K', 'M', 'E'
                    report.Status = request.Status;
                    report.Checkout = null; // Clear checkout
                    if (request.Status == PositionReportStatus.Upload || request.Status == PositionReportStatus.Cancelled)
                    {
                        report.CheckerId = currentUserId;
                        report.CheckedDate = serverTime;
                    }
                    else
                    {
                        report.CheckerId = null;
                        report.CheckedDate = null;
                    }
                    await _auditLogRepository.LogEntryAsync(AuditConstants.ActionUpdateStatus, $"Position report {report.Uid} status updated to {request.Status} by {currentUserId}.", currentUserId);
                }

                await _positionReportRepository.UpdateAsync(report);
            }

            await _positionReportRepository.SaveAsync();

            return allSucceeded;
        }

        public async Task CheckoutPositionReportsAsync(Guid[] uids, string currentDeptCode, string currentUserId)
        {
            _logger.LogInformation("Checking out {Count} position reports for {UserId} in {DeptCode}.", uids.Length, currentUserId, currentDeptCode);

            var sessionId = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 10); // Generate a 10-char session ID
            var checkoutValue = $"{currentUserId}.{sessionId}";

            var reports = await _positionReportRepository.GetManyByIdsAndDepartmentAsync(uids, currentDeptCode);

            if (reports == null || !reports.Any())
            {
                throw new NotFoundException("One or more position reports not found or not in your department.");
            }

            foreach (var report in reports)
            {
                if (string.IsNullOrEmpty(report.Checkout)) // Only allow checkout if not already checked out
                {
                    report.Checkout = checkoutValue;
                    report.CheckoutSessionId = sessionId; // Store just the session ID for lookup
                    await _positionReportRepository.UpdateAsync(report);
                    await _auditLogRepository.LogEntryAsync(AuditConstants.ActionCheckout, $"Position report {report.Uid} checked out by {currentUserId}.", currentUserId);
                }
            }
            await _positionReportRepository.SaveAsync();
        }

        public async Task CheckinPositionReportsAsync(Guid[] uids, string currentDeptCode, string currentUserId)
        {
            _logger.LogInformation("Checking in {Count} position reports for {UserId} in {DeptCode}.", uids.Length, currentUserId, currentDeptCode);

            var reports = await _positionReportRepository.GetManyByIdsAndDepartmentAsync(uids, currentDeptCode);

            if (reports == null || !reports.Any())
            {
                throw new NotFoundException("One or more position reports not found or not in your department.");
            }

            foreach (var report in reports)
            {
                // Only allow checkin if checked out by the current user
                if (report.Checkout?.StartsWith($"{currentUserId}.") ?? false)
                {
                    report.Checkout = null;
                    report.CheckoutSessionId = null;
                    await _positionReportRepository.UpdateAsync(report);
                    await _auditLogRepository.LogEntryAsync(AuditConstants.ActionCheckin, $"Position report {report.Uid} checked in by {currentUserId}.", currentUserId);
                }
            }
            await _positionReportRepository.SaveAsync();
        }

        public async Task LogicallyDeletePositionReportAsync(Guid uid, string currentDeptCode, string currentUserId)
        {
            _logger.LogInformation("Logically deleting position report {Uid} by {UserId} in {DeptCode}.", uid, currentUserId, currentDeptCode);

            var report = await _positionReportRepository.GetByIdAsync(uid);
            if (report == null || report.Dept != currentDeptCode)
            {
                throw new NotFoundException($"Position report with UID {uid} not found or you do not have access to it.");
            }

            // As per Delphi 1.4.0 change: update status to 'X' and prepend char(1) to dept
            report.Status = PositionReportStatus.Deleted;
            report.Dept = AppConstants.LogicalDeletedDeptPrefix + report.Dept;
            report.Checkout = null; // Clear checkout
            report.CheckerId = currentUserId;
            report.CheckedDate = DateTime.UtcNow;

            await _positionReportRepository.UpdateAsync(report);
            await _positionReportRepository.SaveAsync();

            await _auditLogRepository.LogEntryAsync(AuditConstants.ActionDeletePositionReport, $"Position report {uid} logically deleted by {currentUserId}.", currentUserId);
            _logger.LogInformation("Position report {Uid} logically deleted by {UserId}.", uid, currentUserId);
        }

        public async Task<IEnumerable<DuplicatePositionReportDto>> CheckForDuplicatesAsync(DuplicateCheckRequest request, string currentDeptCode, string currentUserId)
        {
            _logger.LogInformation("Checking for duplicates for a transaction in {DeptCode} by {UserId}.", currentDeptCode, currentUserId);
            return await CheckForDuplicatesInternalAsync(request, currentDeptCode, currentUserId);
        }

        private async Task<IEnumerable<DuplicatePositionReportDto>> CheckForDuplicatesInternalAsync(DuplicateCheckRequest request, string currentDeptCode, string currentUserId)
        {
            // The Delphi logic for duplicate check: value_date > ([@GETDATE]-14)
            var minDate = request.ValueDate.AddDays(-_duplicateCheckDays);

            var duplicates = await _positionReportRepository.FindDuplicatesAsync(
                request.Dept,
                request.DrAcct, request.DrCur, request.DrAmount,
                request.CrAcct, request.CrCur, request.CrAmount,
                request.ValueDate, minDate, request.Uid
            );
            return _mapper.Map<IEnumerable<DuplicatePositionReportDto>>(duplicates);
        }

        public async Task<IEnumerable<string>> GetNostroAccountsAsync(string currencyCode)
        {
            _logger.LogInformation("Fetching Nostro accounts for currency {CurrencyCode}.", currencyCode);
            var nostroAccounts = await _positionReportRepository.GetNostroAccountsByCurrencyAsync(currencyCode);
            return nostroAccounts;
        }

        public async Task<CurrencyDetailsDto?> GetCurrencyDetailsAsync(string currencyCode)
        {
            _logger.LogInformation("Fetching currency details for {CurrencyCode}.", currencyCode);
            var currency = await _currencyRepository.GetByCodeAsync(currencyCode);
            return _mapper.Map<CurrencyDetailsDto?>(currency);
        }

        public async Task<string?> GetCurrencyForAccountAsync(string accountNumber)
        {
            _logger.LogInformation("Fetching currency for account {AccountNumber}.", accountNumber);
            // This logic needs to be determined based on how custfile table stores currency per account.
            // For now, assuming a direct lookup.
            var customer = await _customerRepository.GetCustomerByAccountNumberOrAbbrvAsync(accountNumber);
            // Assuming CurrencyCode is a field in Customer entity or can be derived.
            // This is a placeholder, needs actual database schema knowledge.
            return customer?.HomeCurrency; // Assuming a 'HomeCurrency' field exists or similar
        }


        // --- Helper validation method ---
        private async Task ValidatePositionReport(CreatePositionReportRequestBase request, string deptCode)
        {
            if (string.IsNullOrWhiteSpace(request.Dept)) throw new ValidationException("Department field cannot be empty.");
            if (string.IsNullOrWhiteSpace(request.Reference)) throw new ValidationException("Reference number cannot be blank.");

            if (request.Rate <= 0) throw new ValidationException("Exchange rate cannot be zero or lesser.");

            // Determine transaction type
            var drAcctIsNumeric = long.TryParse(request.DrAcct, out _);
            var crAcctIsNumeric = long.TryParse(request.CrAcct, out _);

            TransactionType determinedType;
            if ((!drAcctIsNumeric && !string.IsNullOrWhiteSpace(request.DrAcct)) && (!crAcctIsNumeric && !string.IsNullOrWhiteSpace(request.CrAcct)))
            {
                determinedType = TransactionType.THRU;
            }
            else if ((crAcctIsNumeric || string.IsNullOrWhiteSpace(request.CrAcct))) // Changed logic as per Delphi v1.4.0 (empty is numeric)
            {
                determinedType = TransactionType.INW;
            }
            else // if (drAcctIsNumeric || string.IsNullOrWhiteSpace(request.DrAcct))
            {
                determinedType = TransactionType.OUTW;
            }

            if (request.Type == TransactionType.FE_EXCH)
            {
                // FE_EXCH overrides other logic if chkFEEXCH is checked in UI.
                // Ensure this is properly set by UI and only used for info.
            }
            else if (request.Type != determinedType)
            {
                // This implies the UI calculated the type incorrectly or an override was not FE_EXCH.
                // For now, allow UI's explicit type unless FE_EXCH flag is not set.
                // If this is strict, uncomment below.
                // throw new ValidationException($"Transaction type mismatch. Expected {determinedType}, got {request.Type}.");
            }


            if (string.IsNullOrWhiteSpace(request.DrCur)) throw new ValidationException("Debit currency cannot be empty.");
            if (string.IsNullOrWhiteSpace(request.CrCur)) throw new ValidationException("Credit currency cannot be empty.");
            if (request.DrAmount <= 0) throw new ValidationException("Debit amount cannot be zero.");
            if (request.CrAmount <= 0) throw new ValidationException("Credit amount cannot be zero.");

            if (!Enum.IsDefined(typeof(TransactionType), request.Type)) throw new ValidationException("Invalid transaction type.");
            if (request.DrAmount == 0 && request.CrAmount == 0) throw new ValidationException("Debit or credit amount must be greater than zero.");
            if (string.IsNullOrWhiteSpace(request.Calc)) throw new ValidationException("Calculation symbol cannot be empty.");

            if (request.CrAcct == AppConstants.NoCustomerName && request.DrAcct == AppConstants.NoCustomerName)
            {
                throw new ValidationException("You must enter at least one Nostro account.");
            }

            // Check if department is closed based on transaction date
            var deptStatus = await _departmentRepository.GetByDeptCodeAsync(request.Dept);
            if (deptStatus == null)
            {
                throw new NotFoundException($"Department {request.Dept} not found.");
            }

            if (deptStatus.IsClosedForDate(request.TransDate))
            {
                throw new ValidationException($"The system is closed for new entries for items dated {request.TransDate:yyyyMMdd} or earlier in department {request.Dept}.");
            }

            // Value Date vs Entry Date (TransDate) check
            if (request.TransDate.Date < request.ValueDate.Date && request.Type == TransactionType.INW)
            {
                // Delphi asks for confirmation, API should enforce or ask for override flag
                // For now, consider this a warning that might need UI confirmation.
                _logger.LogWarning("Input entry date {TransDate} is earlier than the Value Date {ValueDate} for INW transaction.", request.TransDate, request.ValueDate);
            }

            if (request.TransDate.Date == default(DateTime).Date && request.ValueDate.Date < DateTime.Today.Date)
            {
                // Value Date is earlier than current date when TransDate is empty (initial entry)
                _logger.LogWarning("Value Date {ValueDate} is earlier than current date for an initial transaction.", request.ValueDate);
            }
        }
    }
}