using PositionReporting.Infrastructure.Data.Entities;
using Microsoft.Kiota.Abstractions; // For Date
using Kiota.ApiClient.Models; // For DuplicateCheckRequest

namespace PositionReporting.Infrastructure.Repositories.Interfaces;

public interface IPositionEntryRepository
{
    Task<List<PositionEntry>> GetAllAsync(
        int? limit, int? offset, string? sort, string? filter,
        string? departmentId, Date? businessDate, List<string>? status);
    Task<PositionEntry?> GetByIdAsync(string id);
    Task AddAsync(PositionEntry entry);
    Task UpdateAsync(PositionEntry entry);
    Task DeleteAsync(string id);
    Task<List<PositionEntry>> GetAwaitingConfirmationEntriesAsync(
        int? limit, int? offset, string? sort, string? filter, string departmentId);
    Task<List<PositionEntry>> GetCorrectionCandidatesAsync(
        int? limit, int? offset, string? sort, string? filter, string departmentId);
    Task<List<DuplicateCheckResult>> CheckDuplicatesAsync(Kiota.ApiClient.Models.DuplicateCheckRequest request);
    Task<IEnumerable<PositionEntry>> GetOutstandingEntriesForDepartmentAsync(string departmentCode, DateTime currentDate);
    Task<bool> HasActiveEntriesForDepartmentAsync(string departmentCode);
    int GetNextReferenceSequence(string departmentId);
}