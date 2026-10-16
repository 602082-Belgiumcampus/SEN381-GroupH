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
    // /Requests/Details is open to any signed-in user; the page itself checks who may see the request.
});

var app = builder.Build();

// Apply migrations and seed roles and categories. Always in Development; elsewhere only when
// Database:MigrateOnStartup is true. Demo users are only ever created in Development.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    var demoPassword = app.Environment.IsDevelopment() ? app.Configuration["Seed:DemoPassword"] : null;
    await initializer.InitialiseAsync(demoPassword);
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
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
