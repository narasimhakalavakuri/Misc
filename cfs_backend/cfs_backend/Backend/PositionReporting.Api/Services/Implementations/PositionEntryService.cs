using Kiota.ApiClient.Models;
using PositionReporting.Api.Controllers; // For PositionEntryListRequest
using PositionReporting.Api.Services.Interfaces;
using PositionReporting.Infrastructure.Data.Entities;
using PositionReporting.Infrastructure.Repositories.Interfaces;
using Microsoft.Kiota.Abstractions; // For Date
using System.Net; // For HttpStatusCode
using System.Security.Cryptography; // For random string

namespace PositionReporting.Api.Services.Implementations;

public class PositionEntryService : IPositionEntryService
{
    private readonly IPositionEntryRepository _positionEntryRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ICurrencyRepository _currencyRepository;
    private readonly ILogger<PositionEntryService> _logger;
    private readonly IClock _clock;

    public PositionEntryService(
        IPositionEntryRepository positionEntryRepository,
        IDepartmentRepository departmentRepository,
        ICurrencyRepository currencyRepository,
        ILogger<PositionEntryService> logger,
        IClock clock)
    {
        _positionEntryRepository = positionEntryRepository;
        _departmentRepository = departmentRepository;
        _currencyRepository = currencyRepository;
        _logger = logger;
        _clock = clock;
    }

    public async Task<List<PositionEntrySummary>> ListPositionEntriesAsync(PositionEntryListRequest request)
    {
        _logger.LogInformation("Listing position entries for department: {DepartmentId}", request.DepartmentId);
        var entities = await _positionEntryRepository.GetAllAsync(
            request.Limit, request.Offset, request.Sort, request.Filter,
            request.DepartmentId, request.BusinessDate, request.Status);
        return entities.Select(MapToKiotaPositionEntrySummary).ToList();
    }

    public async Task<List<PositionEntrySummary>> ListAwaitingConfirmationEntriesAsync(PositionEntryListRequest request)
    {
        _logger.LogInformation("Listing awaiting confirmation entries for department: {DepartmentId}", request.DepartmentId);
        // Business logic from Delphi: trans_date is null and status is not 'K' or 'E'.
        // Simplified by assuming 'M' status and null EntryDate.
        var entities = await _positionEntryRepository.GetAwaitingConfirmationEntriesAsync(
            request.Limit, request.Offset, request.Sort, request.Filter, request.DepartmentId);
        return entities.Select(MapToKiotaPositionEntrySummary).ToList();
    }

    public async Task<List<PositionEntrySummary>> ListCorrectionCandidatesAsync(PositionEntryListRequest request)
    {
        _logger.LogInformation("Listing correction candidates for department: {DepartmentId}", request.DepartmentId);
        // Business logic from Delphi: Status is 'E' (Error/Correction) and not checked out.
        var entities = await _positionEntryRepository.GetCorrectionCandidatesAsync(
            request.Limit, request.Offset, request.Sort, request.Filter, request.DepartmentId);
        return entities.Select(MapToKiotaPositionEntrySummary).ToList();
    }

    public async Task<PositionEntry> CreatePositionEntryAsync(PositionEntryCreateRequest request, string currentUserId)
    {
        _logger.LogInformation("Creating new position entry for department: {DepartmentId}", request.DepartmentId);

        // --- Business Logic & Validation ---
        await ValidatePositionEntry(request.DepartmentId, currentUserId);
        await ValidateCurrenciesAndAccounts(request);
        var transactionType = DetermineTransactionType(request.DebitAccount, request.CreditAccount, request.IsForeignExchange ?? false);
        var homeCurrency = await _currencyRepository.GetHomeCurrencyCodeAsync(); // Assume this exists

        // Auto-generate ID and Reference Number
        var uid = GenerateUniqueId();
        var referenceNumber = GenerateReferenceNumber(request.DepartmentId); // Assumes sequence from DB
        
        // Populate system-controlled fields
        var newEntity = new Infrastructure.Data.Entities.PositionEntry
        {
            Id = uid,
            DepartmentId = request.DepartmentId,
            TransactionType = transactionType.ToString(),
            ReferenceNumber = referenceNumber,
            EntryDate = _clock.UtcNow, // System date/time
            ValueDate = request.ValueDate!.Value.ToDateTime(TimeOnly.MinValue).Date, // Value date
            IssueDate = _clock.UtcNow,
            TheirReference = request.TheirReference,
            Reference = request.Reference,
            DebitAccount = request.DebitAccount,
            DebitAccountName = await GetAccountNameAsync(request.DebitAccount),
            DebitCurrency = request.DebitCurrency,
            DebitAmount = request.DebitAmount ?? 0,
            CreditAccount = request.CreditAccount,
            CreditAccountName = await GetAccountNameAsync(request.CreditAccount),
            CreditCurrency = request.CreditCurrency,
            CreditAmount = request.CreditAmount ?? 0,
            CalculationSymbol = request.CalculationSymbol.ToString(),
            ExchangeRate = request.ExchangeRate ?? 0,
            Status = PositionEntry_status.M.ToString(), // Initial status: Marked/Unchecked
            MakerId = currentUserId,
            CreatedAt = _clock.UtcNow
        };

        // Validate consistency (DR_AMT * RATE = CR_AMT or DR_AMT / RATE = CR_AMT)
        ValidateAmountsConsistency(newEntity);

        await _positionEntryRepository.AddAsync(newEntity);
        _logger.LogInformation("Position entry '{Id}' created successfully.", newEntity.Id);
        return MapToKiotaPositionEntry(newEntity);
    }

    public async Task<PositionEntry?> GetPositionEntryByIdAsync(string positionEntryId, string currentUserId)
    {
        _logger.LogInformation("Getting position entry '{PositionEntryId}'", positionEntryId);
        var entity = await _positionEntryRepository.GetByIdAsync(positionEntryId);
        if (entity == null) return null;

        // Business logic: Entry must not be checked out by another user
        if (!string.IsNullOrEmpty(entity.CheckoutId) && entity.CheckoutId != currentUserId)
        {
            throw new BadHttpRequestException($"Position entry '{positionEntryId}' is currently checked out by another user.", (int)HttpStatusCode.Conflict);
        }

        return MapToKiotaPositionEntry(entity);
    }

    public async Task<PositionEntry?> UpdatePositionEntryAsync(string positionEntryId, PositionEntryUpdateRequest request, string currentUserId)
    {
        _logger.LogInformation("Updating position entry '{PositionEntryId}'", positionEntryId);
        var entity = await _positionEntryRepository.GetByIdAsync(positionEntryId);
        if (entity == null) return null;

        // --- Business Logic & Validation ---
        // 1. Must be checked out by current user
        if (entity.CheckoutId != currentUserId)
        {
            throw new BadHttpRequestException($"Position entry '{positionEntryId}' must be checked out by the current user to update.", (int)HttpStatusCode.Conflict);
        }
        // 2. Cannot correct after approval without specific permissions (assuming not allowed generally here)
        if (entity.Status == PositionEntry_status.U.ToString())
        {
            throw new BadHttpRequestException($"Position entry '{positionEntryId}' cannot be corrected after approval.", (int)HttpStatusCode.Conflict);
        }

        await ValidateCurrenciesAndAccounts(request);
        var transactionType = DetermineTransactionType(request.DebitAccount, request.CreditAccount, request.IsForeignExchange ?? false);

        // Update fields
        entity.DepartmentId = request.DepartmentId;
        entity.TransactionType = transactionType.ToString();
        entity.ValueDate = request.ValueDate!.Value.ToDateTime(TimeOnly.MinValue).Date;
        entity.TheirReference = request.TheirReference;
        entity.Reference = request.Reference;
        entity.DebitAccount = request.DebitAccount;
        entity.DebitAccountName = await GetAccountNameAsync(request.DebitAccount);
        entity.DebitCurrency = request.DebitCurrency;
        entity.DebitAmount = request.DebitAmount ?? 0;
        entity.CreditAccount = request.CreditAccount;
        entity.CreditAccountName = await GetAccountNameAsync(request.CreditAccount);
        entity.CreditCurrency = request.CreditCurrency;
        entity.CreditAmount = request.CreditAmount ?? 0;
        entity.CalculationSymbol = request.CalculationSymbol.ToString();
        entity.ExchangeRate = request.ExchangeRate ?? 0;
        entity.Status = PositionEntry_status.M.ToString(); // Reset status to 'Marked' after correction
        entity.MakerId = currentUserId; // Maker is the one who corrected
        entity.CorrectionDate = _clock.UtcNow;
        entity.CheckoutId = null; // Check-in after update
        entity.UpdatedAt = _clock.UtcNow;

        // Validate consistency (DR_AMT * RATE = CR_AMT or DR_AMT / RATE = CR_AMT)
        ValidateAmountsConsistency(entity);

        await _positionEntryRepository.UpdateAsync(entity);
        _logger.LogInformation("Position entry '{Id}' updated successfully.", entity.Id);
        return MapToKiotaPositionEntry(entity);
    }

    public async Task DeletePositionEntryAsync(string positionEntryId, string currentUserId)
    {
        _logger.LogInformation("Logically deleting position entry '{PositionEntryId}'", positionEntryId);
        var entity = await _positionEntryRepository.GetByIdAsync(positionEntryId);
        if (entity == null)
        {
            throw new BadHttpRequestException($"Position entry '{positionEntryId}' not found.", (int)HttpStatusCode.NotFound);
        }

        // Business logic: Must be checked out by current user
        if (entity.CheckoutId != currentUserId)
        {
            throw new BadHttpRequestException($"Position entry '{positionEntryId}' must be checked out by the current user to delete.", (int)HttpStatusCode.Conflict);
        }

        // Logical delete: Status to 'X' and modify departmentId (Delphi logic)
        entity.Status = PositionEntry_status.X.ToString();
        entity.DepartmentId = "~" + entity.DepartmentId; // Prefix to indicate deleted
        entity.CheckoutId = null; // Check-in after delete
        entity.UpdatedAt = _clock.UtcNow;

        await _positionEntryRepository.UpdateAsync(entity);
        _logger.LogInformation("Position entry '{Id}' logically deleted.", entity.Id);
    }

    public async Task<List<DuplicateCheckResponse>> CheckPositionEntryDuplicatesAsync(DuplicateCheckRequest request)
    {
        _logger.LogInformation("Checking for duplicates for department: {DepartmentId}", request.DepartmentId);
        
        // --- Business Logic: Mimic `sql_check_duplicate` logic from legacy system ---
        var duplicates = await _positionEntryRepository.CheckDuplicatesAsync(request);

        return duplicates.Select(MapToKiotaDuplicateCheckResponse).ToList();
    }

    public async Task<PositionEntry?> UpdatePositionEntryStatusAsync(string positionEntryId, string newStatusString, string currentUserId)
    {
        _logger.LogInformation("Updating status of position entry '{PositionEntryId}' to '{NewStatus}'", positionEntryId, newStatusString);
        var entity = await _positionEntryRepository.GetByIdAsync(positionEntryId);
        if (entity == null) return null;

        if (!Enum.TryParse<PositionEntry_status>(newStatusString, true, out var newStatus))
        {
            throw new BadHttpRequestException($"Invalid status value: {newStatusString}.", (int)HttpStatusCode.BadRequest);
        }

        // --- Business Logic: System Close Check ---
        var department = await _departmentRepository.GetByCodeAsync(entity.DepartmentId);
        if (department != null && department.Status == Department_status.CLOSED.ToString())
        {
            throw new BadHttpRequestException($"The system is closed for department '{entity.DepartmentId}', status update cannot be performed.", (int)HttpStatusCode.Conflict);
        }

        // Update checkerId and checkedDate if status changes to an 'approved' state
        if (newStatus == PositionEntry_status.U)
        {
            entity.CheckerId = currentUserId;
            entity.ApprovedDate = _clock.UtcNow;
            _logger.LogInformation("Position entry '{Id}' approved by {UserId}.", entity.Id, currentUserId);
        }
        else if (newStatus == PositionEntry_status.M) // If changing back to Unchecked
        {
            entity.CheckerId = null;
            entity.ApprovedDate = null;
            _logger.LogInformation("Position entry '{Id}' set to unchecked by {UserId}.", entity.Id, currentUserId);
        }

        entity.Status = newStatusString;
        entity.UpdatedAt = _clock.UtcNow;

        await _positionEntryRepository.UpdateAsync(entity);
        return MapToKiotaPositionEntry(entity);
    }

    public async Task<PositionEntry?> CheckoutPositionEntryAsync(string positionEntryId, string action, string currentUserId)
    {
        _logger.LogInformation("{Action} position entry '{PositionEntryId}' by {UserId}", action, positionEntryId, currentUserId);
        var entity = await _positionEntryRepository.GetByIdAsync(positionEntryId);
        if (entity == null) return null;

        if (action.Equals("checkout", StringComparison.OrdinalIgnoreCase))
        {
            // Business logic: Lock for editing. Fail if already checked out by another user.
            if (!string.IsNullOrEmpty(entity.CheckoutId) && entity.CheckoutId != currentUserId)
            {
                throw new BadHttpRequestException($"Position entry '{positionEntryId}' is already checked out by another user: '{entity.CheckoutId}'.", (int)HttpStatusCode.Conflict);
            }
            if (!string.IsNullOrEmpty(entity.CheckoutId) && entity.CheckoutId == currentUserId)
            {
                // Already checked out by current user, idempotent.
                _logger.LogInformation("Position entry '{Id}' already checked out by current user. No change.", entity.Id);
                return MapToKiotaPositionEntry(entity);
            }

            entity.CheckoutId = $"{currentUserId}.{GenerateRandomString(10)}"; // Format: USERID.RANDOMSTRING
            _logger.LogInformation("Position entry '{Id}' checked out by {UserId}.", entity.Id, currentUserId);
        }
        else if (action.Equals("checkin", StringComparison.OrdinalIgnoreCase))
        {
            // Business logic: Unlock. Fail if checked out by another user.
            if (string.IsNullOrEmpty(entity.CheckoutId))
            {
                // Already checked in, idempotent.
                _logger.LogInformation("Position entry '{Id}' already checked in. No change.", entity.Id);
                return MapToKiotaPositionEntry(entity);
            }
            if (entity.CheckoutId != currentUserId)
            {
                throw new BadHttpRequestException($"Cannot check in an entry '{positionEntryId}' that is checked out by another user: '{entity.CheckoutId}'.", (int)HttpStatusCode.Conflict);
            }

            entity.CheckoutId = null;
            _logger.LogInformation("Position entry '{Id}' checked in by {UserId}.", entity.Id, currentUserId);
        }
        else
        {
            throw new BadHttpRequestException($"Invalid checkout action: {action}.", (int)HttpStatusCode.BadRequest);
        }

        entity.UpdatedAt = _clock.UtcNow;
        await _positionEntryRepository.UpdateAsync(entity);
        return MapToKiotaPositionEntry(entity);
    }

    public async Task<PositionEntry?> UnlockPositionEntryAsync(string positionEntryId)
    {
        _logger.LogInformation("Force-unlocking position entry '{PositionEntryId}'", positionEntryId);
        var entity = await _positionEntryRepository.GetByIdAsync(positionEntryId);
        if (entity == null) return null;

        if (!string.IsNullOrEmpty(entity.CheckoutId))
        {
            entity.CheckoutId = null;
            entity.UpdatedAt = _clock.UtcNow;
            await _positionEntryRepository.UpdateAsync(entity);
            _logger.LogInformation("Position entry '{Id}' force-unlocked.", entity.Id);
        }
        else
        {
            _logger.LogInformation("Position entry '{Id}' was not checked out. No action needed.", entity.Id);
        }
        
        return MapToKiotaPositionEntry(entity);
    }

    // --- Helper Methods (Business Logic) ---
    private string GenerateUniqueId()
    {
        // Matches legacy Delphi format: YYYYMMDDHHMMSS + 5-digit random + - + GetTickCount (simulated with Guid suffix)
        // This is a simplified version, as GetTickCount is not robust across distributed systems.
        // Using GUID for uniqueness.
        return $"{_clock.UtcNow:yyyyMMddHHmmss}{GenerateRandomString(5)}-{Guid.NewGuid().ToString("N")[..10]}";
    }

    private string GenerateReferenceNumber(string departmentId)
    {
        // Mimics Delphi format: DEPTCODE + YYYY + 6-digit sequence number
        var nextSequence = _positionEntryRepository.GetNextReferenceSequence(departmentId); // This would query/update a sequence table
        return $"{departmentId}{_clock.UtcNow:yyyy}{nextSequence:D6}";
    }

    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[RandomNumberGenerator.GetInt32(s.Length)]).ToArray());
    }

    private async Task<string?> GetAccountNameAsync(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber)) return null;
        var account = await _currencyRepository.GetCustomerAccountAsync(accountNumber);
        return account?.AccountName ?? "[NO CUSTOMER]";
    }

    private PositionEntry_transactionType DetermineTransactionType(string? debitAccount, string? creditAccount, bool isForeignExchange)
    {
        if (isForeignExchange) return PositionEntry_transactionType.FE_EXCH;

        var isDebitNostro = _currencyRepository.IsNostroAccount(debitAccount); // Assumes this check exists
        var isCreditNostro = _currencyRepository.IsNostroAccount(creditAccount); // Assumes this check exists

        if (isDebitNostro && isCreditNostro) return PositionEntry_transactionType.THRU;
        if (isDebitNostro && !isCreditNostro) return PositionEntry_transactionType.OUTW;
        if (!isDebitNostro && isCreditNostro) return PositionEntry_transactionType.INW;

        // Default or error case, depending on business rules
        return PositionEntry_transactionType.INW; // Defaulting if neither is Nostro, could be THRU or validation error
    }

    private void ValidateAmountsConsistency(Infrastructure.Data.Entities.PositionEntry entry)
    {
        if (entry.DebitAmount <= 0 || entry.CreditAmount <= 0 || entry.ExchangeRate <= 0)
        {
            throw new BadHttpRequestException("Amounts and exchange rate must be positive.", (int)HttpStatusCode.BadRequest);
        }

        var expectedCreditAmount = 0.0f;
        if (entry.CalculationSymbol == "*")
        {
            expectedCreditAmount = entry.DebitAmount * entry.ExchangeRate;
        }
        else if (entry.CalculationSymbol == "/")
        {
            expectedCreditAmount = entry.DebitAmount / entry.ExchangeRate;
        }
        else
        {
            throw new BadHttpRequestException("Invalid calculation symbol. Must be '*' or '/'.", (int)HttpStatusCode.BadRequest);
        }

        // Allow for small floating-point discrepancies
        if (Math.Abs(entry.CreditAmount - expectedCreditAmount) > 0.01) // Tolerance of 0.01
        {
            throw new BadHttpRequestException($"Credit amount ({entry.CreditAmount}) is inconsistent with debit amount ({entry.DebitAmount}), exchange rate ({entry.ExchangeRate}), and calculation symbol ('{entry.CalculationSymbol}'). Expected: {expectedCreditAmount:F2}", (int)HttpStatusCode.UnprocessableEntity);
        }
    }

    private async Task ValidatePositionEntry(string departmentId, string currentUserId)
    {
        // Example: Check if department is closed
        var department = await _departmentRepository.GetByCodeAsync(departmentId);
        if (department == null)
        {
            throw new BadHttpRequestException($"Department '{departmentId}' not found.", (int)HttpStatusCode.NotFound);
        }
        if (department.Status == Department_status.CLOSED.ToString())
        {
            throw new BadHttpRequestException($"The system is closed for department '{departmentId}', new entries cannot be created.", (int)HttpStatusCode.Conflict);
        }
        // Further validation: makerId (currentUserId) exists, etc.
    }

    private async Task ValidateCurrenciesAndAccounts<T>(T request) where T : IPositionEntryRequest
    {
        if (request.DebitCurrency == null || request.CreditCurrency == null)
        {
            throw new BadHttpRequestException("Debit and Credit currencies are required.", (int)HttpStatusCode.BadRequest);
        }

        var debitCurrency = await _currencyRepository.GetByCodeAsync(request.DebitCurrency);
        if (debitCurrency == null)
        {
            throw new BadHttpRequestException($"Invalid debit currency code: {request.DebitCurrency}", (int)HttpStatusCode.BadRequest);
        }

        var creditCurrency = await _currencyRepository.GetByCodeAsync(request.CreditCurrency);
        if (creditCurrency == null)
        {
            throw new BadHttpRequestException($"Invalid credit currency code: {request.CreditCurrency}", (int)HttpStatusCode.BadRequest);
        }

        if ((request.DebitAmount ?? 0) <= 0) throw new BadHttpRequestException("Debit amount must be positive.", (int)HttpStatusCode.BadRequest);
        if ((request.CreditAmount ?? 0) <= 0) throw new BadHttpRequestException("Credit amount must be positive.", (int)HttpStatusCode.BadRequest);
        if ((request.ExchangeRate ?? 0) <= 0) throw new BadHttpRequestException("Exchange rate must be positive.", (int)HttpStatusCode.BadRequest);
    }


    // --- Mapping Functions (Entity to Kiota DTOs) ---
    private PositionEntrySummary MapToKiotaPositionEntrySummary(Infrastructure.Data.Entities.PositionEntry entity)
    {
        return new PositionEntrySummary
        {
            Id = entity.Id,
            DepartmentId = entity.DepartmentId,
            TransactionType = Enum.Parse<PositionEntry_transactionType>(entity.TransactionType, true),
            ReferenceNumber = entity.ReferenceNumber,
            DebitCurrency = entity.DebitCurrency,
            DebitAmount = entity.DebitAmount,
            CreditCurrency = entity.CreditCurrency,
            CreditAmount = entity.CreditAmount,
            EntryDate = entity.EntryDate,
            ValueDate = Date.FromDateTime(entity.ValueDate), // Map DateTime to Kiota.Abstractions.Date
            Status = Enum.Parse<PositionEntry_status>(entity.Status, true),
            MakerId = entity.MakerId,
            CheckerId = entity.CheckerId
        };
    }

    private PositionEntry MapToKiotaPositionEntry(Infrastructure.Data.Entities.PositionEntry entity)
    {
        return new PositionEntry
        {
            Id = entity.Id,
            DepartmentId = entity.DepartmentId,
            TransactionType = Enum.Parse<PositionEntry_transactionType>(entity.TransactionType, true),
            ReferenceNumber = entity.ReferenceNumber,
            EntryDate = entity.EntryDate,
            ValueDate = Date.FromDateTime(entity.ValueDate),
            IssueDate = entity.IssueDate,
            TheirReference = entity.TheirReference,
            Reference = entity.Reference,
            DebitAccount = entity.DebitAccount,
            DebitAccountName = entity.DebitAccountName,
            DebitCurrency = entity.DebitCurrency,
            DebitAmount = entity.DebitAmount,
            CreditAccount = entity.CreditAccount,
            CreditAccountName = entity.CreditAccountName,
            CreditCurrency = entity.CreditCurrency,
            CreditAmount = entity.CreditAmount,
            CalculationSymbol = Enum.Parse<PositionEntry_calculationSymbol>(entity.CalculationSymbol, true),
            ExchangeRate = entity.ExchangeRate,
            Status = Enum.Parse<PositionEntry_status>(entity.Status, true),
            MakerId = entity.MakerId,
            CheckerId = entity.CheckerId,
            CorrectionDate = entity.CorrectionDate,
            CheckoutId = entity.CheckoutId,
            ApprovedDate = entity.ApprovedDate
        };
    }

    private DuplicateCheckResponse MapToKiotaDuplicateCheckResponse(Infrastructure.Data.Entities.DuplicateCheckResult entity)
    {
        return new DuplicateCheckResponse
        {
            Id = entity.Id,
            DebitCurrency = entity.DebitCurrency,
            DebitAmount = entity.DebitAmount,
            Reference = entity.Reference,
            ValueDate = Date.FromDateTime(entity.ValueDate),
            Accounts = entity.Accounts
        };
    }
}

// Interface to allow generic validation of common properties across create and update requests
public interface IPositionEntryRequest
{
    string? DebitCurrency { get; set; }
    double? DebitAmount { get; set; }
    string? CreditCurrency { get; set; }
    double? CreditAmount { get; set; }
    double? ExchangeRate { get; set; }
}
// Implement for Kiota generated models if needed
// public partial class PositionEntryCreateRequest : IPositionEntryRequest { }
// public partial class PositionEntryUpdateRequest : IPositionEntryRequest { }