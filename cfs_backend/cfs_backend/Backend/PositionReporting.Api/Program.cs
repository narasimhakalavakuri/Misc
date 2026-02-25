using System.Security.Claims;
using System.Text;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using PositionReporting.Api.Configuration;
using PositionReporting.Api.Middleware;
using PositionReporting.Api.Security;
using PositionReporting.Api.Services.Implementations;
using PositionReporting.Api.Services.Interfaces;
using PositionReporting.Infrastructure.Data;
using PositionReporting.Infrastructure.Repositories.Implementations;
using PositionReporting.Infrastructure.Repositories.Interfaces;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Configuration;
using Sustainsys.Saml2.Metadata;
using Kiota.ApiClient.Models; // Required for Kiota.ApiClient.Models.ErrorResponse
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;
using System.Text.Json;
using System.Net;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Net.Http;
using Sustainsys.Saml2.Metadata.Providers;
using System.Xml;
using Sustainsys.Saml2.WebSso;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson(); // For Kiota.ApiClient.Models.ErrorResponse JSON serialization

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Zero-Secret Production: Managed Identity for Azure SQL ---
// Configure Azure SQL DbContext with retry-on-failure
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureSQLConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("AzureSQLConnection connection string is not configured.");
    }

    if (builder.Environment.IsProduction())
    {
        // Production: Use System-Assigned Managed Identity for Azure SQL
        // Connection string must use "Authentication=Active Directory Default;"
        options.UseSqlServer(connectionString,
            sqlServerOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null); // Default error numbers for transient failures
            });
    }
    else
    {
        // Local Development: Use DefaultAzureCredential (User Secrets, VS, Azure CLI, etc.)
        options.UseSqlServer(connectionString,
            sqlServerOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }).AddInterceptors(new AzureAdAuthenticationDbConnectionInterceptor(new DefaultAzureCredential()));
    }
});

// Configure strongly-typed SAML settings
builder.Services.Configure<Saml2Settings>(builder.Configuration.GetSection("Saml2"));
var saml2Config = new Saml2Settings();
builder.Configuration.GetSection("Saml2").Bind(saml2Config);

// --- 1. The Identity Bridge (SAML 2.0 to JWT) ---
// Configure Sustainsys.Saml2
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = Saml2Defaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Saml2Defaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "SamlAuthCookie";
    options.Cookie.SameSite = SameSiteMode.None; // Important for cross-site SAML flow
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToLogin = context =>
    {
        // For API, we don't redirect to login page for cookie auth failure
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        return Task.CompletedTask;
    };
})
.AddSaml2(options =>
{
    options.SPOptions.EntityId = new EntityId(saml2Config.ServiceProviderEntityId);
    options.SPOptions.ReturnUrl = new Uri(saml2Config.ServiceProviderReturnUrl);
    options.IdentityProviders.Add(new IdentityProvider(new EntityId(saml2Config.IdentityProviderEntityId), options.SPOptions)
    {
        MetadataLocation = saml2Config.IdentityProviderMetadataUrl,
        LoadMetadataAsync = async (location, options) =>
        {
            using var httpClient = new HttpClient();
            var metadataXml = await httpClient.GetStringAsync(location);
            var xmlReader = XmlReader.Create(new StringReader(metadataXml));
            return MetadataBase.Read(xmlReader, new Saml2Configuration());
        }
    });

    options.Notifications.AcsCommandCreated = (command, context) =>
    {
        // Customize the AcsCommand if needed before execution
        // e.g., command.RelyingParty = "mycustomrelyingparty";
        return Task.CompletedTask;
    };

    options.Notifications.AcsCommandResultCreated = async (commandResult, context) =>
    {
        // Intercept AcquiredClaims for custom logic (Tenant & User Validation, Token Generation)
        await context.HttpContext.RequestServices.GetRequiredService<Saml2PostAuthenticationHandler>().HandleAsync(commandResult, context.HttpContext);
    };

    options.OutboundSigningAlgorithm = SecurityAlgorithms.RsaSha256;
});

// Configure JWT Bearer authentication for API authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ClockSkew = TimeSpan.Zero // No clock skew tolerance for stricter validation
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Add("Token-Expired", "true");
            }
            context.NoResult();
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = MediaTypeNames.Application.Json;
            var problem = new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Message = "Authentication failed.",
                Details = context.Exception.Message
            };
            var json = JsonSerializer.Serialize(problem);
            return context.Response.WriteAsync(json);
        },
        OnChallenge = context =>
        {
            context.HandleResponse(); // Suppress the default challenge response
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = MediaTypeNames.Application.Json;
            var problem = new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Message = "Authentication required.",
                Details = "No valid JWT provided or access token expired."
            };
            var json = JsonSerializer.Serialize(problem);
            return context.Response.WriteAsync(json);
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = MediaTypeNames.Application.Json;
            var problem = new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Forbidden,
                Message = "Access forbidden.",
                Details = "You do not have the necessary permissions to access this resource."
            };
            var json = JsonSerializer.Serialize(problem);
            return context.Response.WriteAsync(json);
        }
    };
});

builder.Services.AddAuthorization();

// Register application services and repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPositionEntryRepository, PositionEntryRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPositionEntryService, PositionEntryService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddScoped<RefreshTokenManager>();
builder.Services.AddScoped<Saml2PostAuthenticationHandler>(); // Register the custom handler

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("AzureSQLConnection")!,
                  name: "Azure SQL Database",
                  failureStatus: HealthStatus.Degraded,
                  tags: new[] { "db", "ready" },
                  timeout: TimeSpan.FromSeconds(30))
    .AddUrlGroup(new Uri(saml2Config.IdentityProviderMetadataUrl),
                 name: "SAML IdP Metadata Endpoint",
                 failureStatus: HealthStatus.Unhealthy,
                 tags: new[] { "saml", "ready" },
                 timeout: TimeSpan.FromSeconds(10));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Keep for detailed errors in development
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // --- Global Exception Handling (RFC 7807 Problem Details) ---
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = MediaTypeNames.Application.Json;

            var problem = new ErrorResponse
            {
                StatusCode = context.Response.StatusCode,
                Message = "An unexpected error occurred.",
                Details = app.Environment.IsDevelopment() ? exception?.ToString() : "Please try again later. If the problem persists, contact support."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        });
    });
    // app.UseMiddleware<ProblemDetailsMiddleware>(); // Alternative for more granular control
    app.UseHsts();
}

app.UseHttpsRedirection();

// SAML specific middleware for handling login/logout
app.UseRouting();

app.UseAuthentication(); // Must be before UseAuthorization
app.UseAuthorization();

// Health Check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                exception = e.Value.Exception?.Message ?? "none",
                duration = e.Value.Duration.ToString()
            })
        }, new JsonSerializerOptions { WriteIndented = true });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = (_) => false, // Only indicates that the app is running
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});

app.MapControllers();

app.Run();

// Static helper for custom health check response writer
public static class HealthCheckResponseWriter
{
    public static Task WriteResponse(HttpContext httpContext, HealthReport result)
    {
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;

        var json = JsonSerializer.Serialize(new
        {
            status = result.Status.ToString(),
            checks = result.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description
                })
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        return httpContext.Response.WriteAsync(json);
    }
}