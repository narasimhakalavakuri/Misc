using Kiota.ApiClient.Models;
using PositionReporting.Api.Controllers; // For DepartmentListRequest
using Microsoft.Kiota.Abstractions; // For Date

namespace PositionReporting.Api.Services.Interfaces;

public interface IDepartmentService
{
    Task<List<Department>> ListDepartmentsAsync(Controllers.DepartmentsController.DepartmentListRequest request);
    Task<Department> CreateDepartmentAsync(DepartmentCreateRequest request);
    Task DeleteDepartmentAsync(string departmentCode);
    Task<DepartmentCloseStatus?> GetDepartmentCloseStatusAsync(string departmentCode);
    Task<DepartmentCloseStatus?> UpdateDepartmentCloseStatusAsync(string departmentCode, bool isClosed, Date? verifyDate, string currentUserId);
}