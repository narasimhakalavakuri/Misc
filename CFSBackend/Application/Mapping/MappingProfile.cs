using AutoMapper;
using ProjectName.Application.Models.Auth;
using ProjectName.Application.Models.Departments;
using ProjectName.Application.Models.PositionReports;
using ProjectName.Application.Models.System;
using ProjectName.Application.Models.Users;
using ProjectName.Domain.Models;
using ProjectName.Infrastructure.Data.Entities;

namespace ProjectName.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User Mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.UserId)); // Simple display name

            CreateMap<CreateUserRequest, User>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Password will be hashed in service
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<UpdateUserRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // UserId cannot be updated via this DTO
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));


            // Department Mappings
            CreateMap<Department, DepartmentDto>();
            CreateMap<CreateDepartmentRequest, Department>()
                .ForMember(dest => dest.RefLock, opt => opt.MapFrom(_ => ".")) // Default value from Delphi
                .ForMember(dest => dest.ClosedDate, opt => opt.MapFrom(_ => (DateTime?)null));

            CreateMap<Department, DepartmentStatusDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.ClosedDate.HasValue && src.ClosedDate.Value.Date == DateTime.Today.Date ? "CLOSED" : "OPEN"))
                .ForMember(dest => dest.LastClosedDate, opt => opt.MapFrom(src => src.ClosedDate));

            // Position Report Mappings
            CreateMap<PositionReport, PositionReportDto>();
            CreateMap<CreatePositionReportRequest, PositionReport>()
                .ForMember(dest => dest.Uid, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => Domain.Enums.PositionReportStatus.Unchecked)) // Default status is 'M' (Unchecked)
                .ForMember(dest => dest.Checkout, opt => opt.MapFrom(_ => ""))
                .ForMember(dest => dest.IssueDate, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<UpdatePositionReportRequest, PositionReport>()
                .ForMember(dest => dest.Uid, opt => opt.Ignore())
                .ForMember(dest => dest.IssueDate, opt => opt.Ignore()) // IssueDate shouldn't be updated here
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => Domain.Enums.PositionReportStatus.Unchecked)) // After correction, status becomes 'M' (Unchecked)
                .ForMember(dest => dest.Checkout, opt => opt.MapFrom(_ => ""))
                .ForMember(dest => dest.CorrectionDate, opt => opt.MapFrom(_ => DateTime.UtcNow));


            CreateMap<PositionReport, PositionReportListItemDto>();
            CreateMap<PositionReport, DuplicatePositionReportDto>()
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.DrCur))
                .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.DrAmount))
                .ForMember(dest => dest.AccountPair, opt => opt.MapFrom(src => $"{src.DrAcct}-{src.CrAcct}"));

            CreateMap<Currency, CurrencyDetailsDto>();

            // JWT Settings (used for configuration)
            CreateMap<IConfiguration, JwtSettings>();
        }
    }
}