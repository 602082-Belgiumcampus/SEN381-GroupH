using CivicConnect.Infrastructure;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Database, ASP.NET Core Identity and the module services
builder.Services.AddCivicConnectInfrastructure(builder.Configuration);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddAuthorization(options =>
{
    // Every page needs a signed-in user unless it is explicitly public (CL-04, NFR-001)
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

    options.AddPolicy(Policies.Requester, policy => policy.RequireRole(nameof(Role.Requester)));
    options.AddPolicy(Policies.StaffQueue, policy => policy.RequireRole(Policies.StaffQueueRoles));
    options.AddPolicy(Policies.LogOnBehalf, policy => policy.RequireRole(Policies.LogOnBehalfRoles));
    options.AddPolicy(Policies.Reports, policy => policy.RequireRole(Policies.ReportRoles));
    options.AddPolicy(Policies.Administrator, policy => policy.RequireRole(nameof(Role.Administrator)));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Error");
    options.Conventions.AllowAnonymousToPage("/Lifecycle");
    options.Conventions.AllowAnonymousToFolder("/Account");

    options.Conventions.AuthorizePage("/Requests/New", Policies.Requester);
    options.Conventions.AuthorizePage("/Requests/Index", Policies.Requester);
    options.Conventions.AuthorizeFolder("/Staff", Policies.StaffQueue);
    options.Conventions.AuthorizePage("/Staff/LogRequest", Policies.LogOnBehalf);
    options.Conventions.AuthorizeFolder("/Reports", Policies.Reports);
    options.Conventions.AuthorizeFolder("/Admin", Policies.Administrator);
    // /Notices is open to any signed-in user; it only ever shows their own notices.
    // /Requests/Details is open to any signed-in user; the page itself checks who may see the request.
});

var app = builder.Build();

// Seed roles and categories on every start (idempotent). Migrations are applied here only in
// Development or when Database:MigrateOnStartup is true; elsewhere they are a separate deployment
// step (D-015) and start-up refuses to run against an out-of-date schema.
// Demo users are only ever created in Development. Seed:AdminEmail / Seed:AdminPassword create the
// first administrator on a new environment such as staging.
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    var demoPassword = app.Environment.IsDevelopment() ? app.Configuration["Seed:DemoPassword"] : null;
    var applyMigrations = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup");
    var adminEmail = app.Configuration["Seed:AdminEmail"];
    var adminPassword = app.Configuration["Seed:AdminPassword"];
    var bootstrapAdmin = !string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrEmpty(adminPassword)
        ? (adminEmail.Trim(), adminPassword)
        : ((string, string)?)null;

    await initializer.InitialiseAsync(demoPassword, applyMigrations, bootstrapAdmin);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// CSS, JS and images must load before sign-in, so they are exempt from the sign-in fallback policy
app.MapStaticAssets().AllowAnonymous();
// Health check for the hosting platform. Reports only Healthy/Unhealthy, no details.
app.MapHealthChecks("/health").AllowAnonymous();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

// Lets the integration tests start the app with WebApplicationFactory<Program>
public partial class Program;