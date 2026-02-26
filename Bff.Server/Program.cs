using Sustainsys.Saml2;
using Sustainsys.Saml2.Metadata;
using Sustainsys.Saml2.AspNetCore2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddAuthentication(o =>
{
    o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = Saml2Defaults.Scheme;
})
.AddCookie(o =>
{
    o.Cookie.Name = "BFF-Auth";
    o.Cookie.SameSite = SameSiteMode.Strict; // Critical for BFF
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
})
.AddSaml2(o =>
{
    o.SPOptions.EntityId = new EntityId(builder.Configuration["Saml2:EntityId"]);



    o.Notifications.AcsCommandResultCreated = (result, api) =>
    {
        Console.WriteLine(">>> BOUNCER: I just caught a SAML message from Azure!");
        Console.WriteLine(result);
        if (result.Principal?.Identity?.IsAuthenticated == true)
        {
            Console.WriteLine(">>> STATUS: Success (Implicitly verified by Sustainsys)");
        }

        if (result.Principal != null)
        {
            Console.WriteLine("-- User Claims --");
            foreach (var claim in result.Principal.Claims)
            {
                Console.WriteLine($"{claim.Type}: {claim.Value}");
            }
        }

        // 2. See the Handshake Details (RelayState, etc.)
        // Console.WriteLine($"Handshake Status: {result.Handled}");
        // Console.WriteLine($"Redirect URL: {result.Location}");

        // return Task.CompletedTask;
        // return Task.CompletedTask;
    };


    var idp = new IdentityProvider(
        new EntityId("https://sts.windows.net/48db7f28-cfd4-4f49-8850-d6e96b50c84e/"),
        o.SPOptions
    )
    {
        MetadataLocation = "https://login.microsoftonline.com/48db7f28-cfd4-4f49-8850-d6e96b50c84e/federationmetadata/2007-06/federationmetadata.xml?appid=cde35539-58c9-4b0c-9b33-7274cb2a2ea0",
        LoadMetadata = true
    };

    o.IdentityProviders.Add(idp);
});

builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration["Saml2:ClientUrl"]!)
    .AllowCredentials()
    .AllowAnyMethod()
    .AllowAnyHeader()));

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// 3. Endpoints
app.MapGet("/auth/login", () =>
    Results.Challenge(new AuthenticationProperties { RedirectUri = "https://localhost:5173/dashboard" }));

app.MapGet("/", () =>
    Results.Challenge(new AuthenticationProperties { RedirectUri = "https://localhost:5173/dashboard" }));

app.MapGet("/auth/me", (HttpContext ctx) =>
    ctx.User.Identity?.IsAuthenticated == true
        ? Results.Ok(new { user = ctx.User.Identity.Name })
        : Results.Unauthorized());

app.Run();