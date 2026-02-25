using Kiota.ApiClient.Models;
using PositionReporting.Api.Controllers; // For DepartmentListRequest
using PositionReporting.Api.Services.Interfaces;
using PositionReporting.Infrastructure.Data.Entities;
using PositionReporting.Infrastructure.Repositories.Interfaces;
using System.Net; // For HttpStatusCode

namespace PositionReporting.Api.Services.Implementations;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPositionEntryRepository _positionEntryRepository; // For system close checks
    private readonly ILogger<DepartmentService> _logger;
    private readonly IClock _clock; // Abstraction for DateTime.UtcNow for testability

    public DepartmentService(IDepartmentRepository departmentRepository, IPositionEntryRepository positionEntryRepository, ILogger<DepartmentService> logger, IClock clock)
    {
        _departmentRepository = departmentRepository;
        _positionEntryRepository = positionEntryRepository;
        _logger = logger;
        _clock = clock;
    }

    public async Task<List<Department>> ListDepartmentsAsync(DepartmentListRequest request)
    {
        _logger.LogInformation("Listing departments with filter: {Filter}", request.Filter);
        var entities = await _departmentRepository.GetAllAsync(
            request.Limit,
            request.Offset,
            request.Sort,
            request.Filter,
            request.DepartmentCodePattern);

        return entities.Select(MapToKiotaDepartment).ToList();
    }

    public async Task<Department> CreateDepartmentAsync(DepartmentCreateRequest request)
    {
        _logger.LogInformation("Creating new department: {DepartmentCode}", request.DepartmentCode);

        // Business logic: Check for uniqueness
        var existingDepartment = await _departmentRepository.GetByCodeAsync(request.DepartmentCode);
        if (existingDepartment != null)
        {
            throw new BadHttpRequestException($"Department with code '{request.DepartmentCode}' already exists.", (int)HttpStatusCode.Conflict);
        }

        var newEntity = new Infrastructure.Data.Entities.Department
        {
            DepartmentCode = request.DepartmentCode,
            DepartmentDescription = request.DepartmentDescription,
            CreatedAt = _clock.UtcNow,
            Status = Department_status.OPEN.ToString(), // Default to OPEN
        };

        await _departmentRepository.AddAsync(newEntity);
        return MapToKiotaDepartment(newEntity);
    }

    public async Task DeleteDepartmentAsync(string departmentCode)
    {
        _logger.LogInformation("Deleting department: {DepartmentCode}", departmentCode);
        var existingDepartment = await _departmentRepository.GetByCodeAsync(departmentCode);
        if (existingDepartment == null)
        {
            throw new BadHttpRequestException($"Department with code '{departmentCode}' not found.", (int)HttpStatusCode.NotFound);
        }

        // Business logic: Potentially check for associated position entries before physical deletion
        // For logical deletion, this might be a status update instead of actual delete.
        // Assuming physical deletion based on the spec's "irreversible" comment.
        var hasActiveEntries = await _positionEntryRepository.HasActiveEntriesForDepartmentAsync(departmentCode);
        if (hasActiveEntries)
        {
            throw new BadHttpRequestException($"Cannot delete department '{departmentCode}' as it has active position entries.", (int)HttpStatusCode.Conflict);
        }

        await _departmentRepository.DeleteAsync(departmentCode);
    }

    public async Task<DepartmentCloseStatus?> GetDepartmentCloseStatusAsync(string departmentCode)
    {
        _logger.LogInformation("Getting close status for department: {DepartmentCode}", departmentCode);
        var entity = await _departmentRepository.GetByCodeAsync(departmentCode);
        return entity == null ? null : MapToKiotaDepartmentCloseStatus(entity);
    }

    public async Task<DepartmentCloseStatus?> UpdateDepartmentCloseStatusAsync(string departmentCode, bool isClosed, Microsoft.Kiota.Abstractions.Date? verifyDate, string currentUserId)
    {
        _logger.LogInformation("Updating close status for department {DepartmentCode} to IsClosed: {IsClosed}", departmentCode, isClosed);
        var entity = await _departmentRepository.GetByCodeAsync(departmentCode);
        if (entity == null)
        {
            throw new BadHttpRequestException($"Department with code '{departmentCode}' not found.", (int)HttpStatusCode.NotFound);
        }

        if (isClosed)
        {
            // Business logic for closing:
            // 1. Verify date must match current system date
            if (!verifyDate.HasValue || verifyDate.Value.ToDateTime(TimeOnly.MinValue).Date != _clock.UtcNow.Date)
            {
                throw new BadHttpRequestException("Verify Date must match the current system date to close the department.", (int)HttpStatusCode.BadRequest);
            }

            // 2. Check for outstanding items (SystemCloseCheck in Delphi)
            var outstandingItems = await _positionEntryRepository.GetOutstandingEntriesForDepartmentAsync(departmentCode, _clock.UtcNow.Date);
            if (outstandingItems != null && outstandingItems.Any())
            {
                // Return a specific ProblemDetails error with details about outstanding items
                // This would typically involve custom ProblemDetailsFactory or an exception that maps well.
                // For simplicity, a generic conflict with details for now.
                var details = string.Join("; ", outstandingItems.Select(i => $"{i.TransactionType}, {i.ValueDate:yyyy-MM-dd}, {i.DebitCurrency} {i.DebitAmount}, {i.DebitAccountName ?? i.DebitAccount}"));
                throw new BadHttpRequestException($"Outstanding items (Value Date Due) exist. Verification required. Details: {details}", (int)HttpStatusCode.Conflict);
            }

            entity.ClosedDate = _clock.UtcNow;
            entity.ClosedBy = currentUserId;
            entity.Status = Department_status.CLOSED.ToString();
            _logger.LogInformation("Department {DepartmentCode} closed by {UserId}.", departmentCode, currentUserId);
        }
        else
        {
            // Business logic for opening:
            if (entity.Status == Department_status.OPEN.ToString())
            {
                // Already open, idempotent operation
                _logger.LogInformation("Department {DepartmentCode} is already open. No changes needed.", departmentCode);
                return MapToKiotaDepartmentCloseStatus(entity);
            }
            entity.ClosedDate = null;
            entity.ClosedBy = null;
            entity.Status = Department_status.OPEN.ToString();
            _logger.LogInformation("Department {DepartmentCode} opened by {UserId}.", departmentCode, currentUserId);
        }

        await _departmentRepository.UpdateAsync(entity);
        return MapToKiotaDepartmentCloseStatus(entity);
    }

    private Department MapToKiotaDepartment(Infrastructure.Data.Entities.Department entity)
    {
        return new Department
        {
            DepartmentCode = entity.DepartmentCode,
            DepartmentDescription = entity.DepartmentDescription,
            ClosedDate = entity.ClosedDate,
            ClosedBy = entity.ClosedBy,
            Status = Enum.Parse<Department_status>(entity.Status, true)
        };
    }

    private DepartmentCloseStatus MapToKiotaDepartmentCloseStatus(Infrastructure.Data.Entities.Department entity)
    {
        return new DepartmentCloseStatus
        {
            DepartmentCode = entity.DepartmentCode,
            ClosedDate = entity.ClosedDate,
            ClosedBy = entity.ClosedBy,
            Status = Enum.Parse<Department_status>(entity.Status, true)
        };
    }
}

// Simple Clock interface for testability
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}