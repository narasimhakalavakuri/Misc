using AutoMapper;
using ProjectName.Application.Models.Departments;
using ProjectName.Application.Services.Interfaces;
using ProjectName.Domain.Constants;
using ProjectName.Infrastructure.Data.Entities;
using ProjectName.Infrastructure.Data.Repositories.Interfaces;
using ProjectName.Infrastructure.Exceptions;

namespace ProjectName.Application.Services.Implementations
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DepartmentService> _logger;

        public DepartmentService(IDepartmentRepository departmentRepository, IAuditLogRepository auditLogRepository, IMapper mapper, ILogger<DepartmentService> logger)
        {
            _departmentRepository = departmentRepository;
            _auditLogRepository = auditLogRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync()
        {
            _logger.LogInformation("Fetching all departments.");
            var departments = await _departmentRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<DepartmentDto>>(departments);
        }

        public async Task<DepartmentDto?> GetDepartmentByCodeAsync(string deptCode)
        {
            _logger.LogInformation("Fetching department by code: {DeptCode}", deptCode);
            var department = await _departmentRepository.GetByDeptCodeAsync(deptCode);
            return _mapper.Map<DepartmentDto?>(department);
        }

        public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request)
        {
            _logger.LogInformation("Creating new department with code: {DeptCode}", request.DeptCode);

            // Normalize DeptCode to uppercase as per Delphi app
            var normalizedDeptCode = request.DeptCode.ToUpper();

            if (await _departmentRepository.GetByDeptCodeAsync(normalizedDeptCode) != null)
            {
                throw new ValidationException($"Department with code {normalizedDeptCode} already exists.");
            }

            var department = _mapper.Map<Department>(request);
            department.DeptCode = normalizedDeptCode;
            department.CreatedAt = DateTime.UtcNow;
            department.UpdatedAt = DateTime.UtcNow;

            await _departmentRepository.AddAsync(department);
            await _departmentRepository.SaveAsync();

            await _auditLogRepository.LogEntryAsync(AuditConstants.ActionAddDepartment, $"Department {normalizedDeptCode} created.", "SYSTEM"); // Log as SYSTEM user for admin actions

            _logger.LogInformation("Department {DeptCode} created successfully.", normalizedDeptCode);
            return _mapper.Map<DepartmentDto>(department);
        }

        public async Task<DepartmentDto?> UpdateDepartmentAsync(string deptCode, UpdateDepartmentRequest request)
        {
            _logger.LogInformation("Updating department with code: {DeptCode}", deptCode);

            // Normalize DeptCode to uppercase
            var normalizedDeptCode = deptCode.ToUpper();

            var existingDepartment = await _departmentRepository.GetByDeptCodeAsync(normalizedDeptCode);
            if (existingDepartment == null)
            {
                _logger.LogWarning("Department {DeptCode} not found for update.", normalizedDeptCode);
                throw new NotFoundException($"Department with code {normalizedDeptCode} not found.");
            }

            _mapper.Map(request, existingDepartment);
            existingDepartment.UpdatedAt = DateTime.UtcNow;

            await _departmentRepository.UpdateAsync(existingDepartment);
            await _departmentRepository.SaveAsync();

            await _auditLogRepository.LogEntryAsync(AuditConstants.ActionUpdateDepartment, $"Department {normalizedDeptCode} updated.", "SYSTEM");

            _logger.LogInformation("Department {DeptCode} updated successfully.", normalizedDeptCode);
            return _mapper.Map<DepartmentDto>(existingDepartment);
        }

        public async Task<bool> DeleteDepartmentAsync(string deptCode)
        {
            _logger.LogInformation("Deleting department with code: {DeptCode}", deptCode);

            // Normalize DeptCode to uppercase
            var normalizedDeptCode = deptCode.ToUpper();

            var existingDepartment = await _departmentRepository.GetByDeptCodeAsync(normalizedDeptCode);
            if (existingDepartment == null)
            {
                _logger.LogWarning("Department {DeptCode} not found for deletion.", normalizedDeptCode);
                return false;
            }

            // Check if there are any users assigned to this department
            var hasUsers = await _departmentRepository.HasUsersInDepartment(normalizedDeptCode);
            if (hasUsers)
            {
                throw new ValidationException($"Cannot delete department {normalizedDeptCode} because there are users assigned to it.");
            }

            // Check if there are any position reports associated with this department
            var hasPositionReports = await _departmentRepository.HasPositionReportsInDepartment(normalizedDeptCode);
            if (hasPositionReports)
            {
                throw new ValidationException($"Cannot delete department {normalizedDeptCode} because there are position reports associated with it.");
            }

            await _departmentRepository.DeleteAsync(existingDepartment);
            await _departmentRepository.SaveAsync();

            await _auditLogRepository.LogEntryAsync(AuditConstants.ActionDeleteDepartment, $"Department {normalizedDeptCode} deleted.", "SYSTEM");

            _logger.LogInformation("Department {DeptCode} deleted successfully.", normalizedDeptCode);
            return true;
        }
    }
}