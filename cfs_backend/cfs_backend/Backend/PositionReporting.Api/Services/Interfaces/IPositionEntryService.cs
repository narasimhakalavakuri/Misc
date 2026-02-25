using Kiota.ApiClient.Models;
using PositionReporting.Api.Controllers; // For PositionEntryListRequest
using Microsoft.Kiota.Abstractions; // For Date

namespace PositionReporting.Api.Services.Interfaces;

public interface IPositionEntryService
{
    Task<List<PositionEntrySummary>> ListPositionEntriesAsync(Controllers.PositionEntriesController.PositionEntryListRequest request);
    Task<PositionEntry> CreatePositionEntryAsync(PositionEntryCreateRequest request, string currentUserId);
    Task<PositionEntry?> GetPositionEntryByIdAsync(string positionEntryId, string currentUserId);
    Task<PositionEntry?> UpdatePositionEntryAsync(string positionEntryId, PositionEntryUpdateRequest request, string currentUserId);
    Task DeletePositionEntryAsync(string positionEntryId, string currentUserId);
    Task<List<PositionEntrySummary>> ListAwaitingConfirmationEntriesAsync(Controllers.PositionEntriesController.PositionEntryListRequest request);
    Task<List<PositionEntrySummary>> ListCorrectionCandidatesAsync(Controllers.PositionEntriesController.PositionEntryListRequest request);
    Task<List<DuplicateCheckResponse>> CheckPositionEntryDuplicatesAsync(DuplicateCheckRequest request);
    Task<PositionEntry?> UpdatePositionEntryStatusAsync(string positionEntryId, string newStatusString, string currentUserId);
    Task<PositionEntry?> CheckoutPositionEntryAsync(string positionEntryId, string action, string currentUserId);
    Task<PositionEntry?> UnlockPositionEntryAsync(string positionEntryId);
}