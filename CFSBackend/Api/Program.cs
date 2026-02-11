using Microsoft.EntityFrameworkCore;
using ProjectName.Infrastructure.Data;
using ProjectName.Infrastructure.Middleware;
using ProjectName.Application.Services.Interfaces;
using ProjectName.Application.Services.Implementations;
using ProjectName.Infrastructure.Data.Repositories;
using ProjectName.Infrastructure.Data.Repositories.Interfaces;
using ProjectName.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using AutoMapper;
using ProjectName.Application.Mapping;
using ProjectName.Domain.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Cash Flow System API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Configure PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Register Repositories
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IPositionReportRepository, PositionReportRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IPositionReportService, PositionReportService>();
builder.Services.AddScoped<ISystemService, SystemService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>(); // Inject the password hasher

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
builder.Services.AddSingleton(jwtSettings!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings!.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
});

// Add Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanQuery", policy => policy.RequireClaim("AccessMask", mask => mask != null && mask.Length > 0 && mask[0] == '1'));
    options.AddPolicy("CanInput", policy => policy.RequireClaim("AccessMask", mask => mask != null && mask.Length > 1 && mask[1] == '1'));
    options.AddPolicy("CanCheck", policy => policy.RequireClaim("AccessMask", mask => mask != null && mask.Length > 2 && mask[2] == '1'));
    options.AddPolicy("CanAdmin", policy => policy.RequireClaim("AccessMask", mask => mask != null && mask.Length > 3 && mask[3] == '1'));
    options.AddPolicy("CanUserAdmin", policy => policy.RequireClaim("AccessMask", mask => mask != null && mask.Length > 4 && mask[4] == '1'));
    options.AddPolicy("CanReport", policy => policy.RequireClaim("AccessMask", mask => mask != null && mask.Length > 5 && mask[5] == '1'));
    options.AddPolicy("CanSystemControl", policy => policy.RequireClaim("AccessMask", mask => mask != null && mask.Length > 6 && mask[6] == '1'));
});


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["CorsOrigins"]?.Split(',') ?? Array.Empty<string>())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // If you are using cookies/credentials with your frontend
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging(); // Add Serilog request logging

app.UseHttpsRedirection();

app.UseCors(); // Use the CORS policy

app.UseMiddleware<GlobalExceptionHandlingMiddleware>(); // Custom exception handling middleware

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();

    // Seed initial admin user if needed
    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        if (!dbContext.Users.Any())
        {
            logger.LogInformation("No users found. Prompting for initial admin user setup.");
            Console.WriteLine("The user table is empty. Please enter the fully qualified admin user ID (e.g., DOMAIN\\username):");
            var adminUserId = Console.ReadLine()?.Trim();
            Console.WriteLine("Enter initial password for admin user:");
            var adminPassword = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(adminUserId) && !string.IsNullOrWhiteSpace(adminPassword))
            {
                var accessMask = new string('1', 7) + new string('0', 20); // Full access
                await userService.CreateInitialAdminUserAsync(adminUserId, adminPassword, accessMask);
                logger.LogInformation("Initial admin user created successfully.");
            }
            else
            {  logger.LogWarning("Admin user ID or password cannot be empty. Skipping initial admin setup."); }
        }

        if (!dbContext.Departments.Any())
        {
             logger.LogInformation("No departments found. Seeding default department.");
             await userService.SeedDefaultDepartments();
        }

        if (!dbContext.Currencies.Any())
        {
            logger.LogInformation("No currencies found. Seeding default currencies.");
            await userService.SeedDefaultCurrencies();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying migrations or seeding the database.");
    }
}


app.Run();

public partial class Program { } // For testing purposes