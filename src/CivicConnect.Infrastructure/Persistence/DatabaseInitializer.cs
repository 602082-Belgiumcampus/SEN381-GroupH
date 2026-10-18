using CivicConnect.Infrastructure.Identity;
using CivicConnect.Modules.IdentityAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CivicConnect.Infrastructure.Persistence;

/// <summary>
/// Applies EF Core migrations and seeds the reference data the app cannot run without
/// (roles and an initial category list, CR-003 "seed first"). Optionally adds one demo user per
/// role for local development; the demo password comes from configuration, never from the code.
/// </summary>
public sealed class DatabaseInitializer(
    CivicConnectDbContext db,
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<AppUser> userManager,
    TimeProvider time,
    ILogger<DatabaseInitializer> logger)
{
    private static readonly (string Name, int TargetDays)[] InitialCategories =
    [
        ("Facilities and maintenance", 10),
        ("Cleaning and waste", 5),
        ("Safety and security", 2),
        ("Programmes and events", 14),
        ("Other", 10)
    ];

    private static readonly (string Email, string DisplayName, Role Role)[] DemoUsers =
    [
        ("requester@civicconnect.local", "Demo Requester", Role.Requester),
        ("staff@civicconnect.local", "Demo Staff Member", Role.ServiceStaff),
        ("supervisor@civicconnect.local", "Demo Supervisor", Role.Supervisor),
        ("manager@civicconnect.local", "Demo Manager", Role.Management),
        ("admin@civicconnect.local", "Demo Administrator", Role.Administrator)
    ];

    /// <summary>
    /// <paramref name="applyMigrations"/> is true in Development. Elsewhere migrations are a separate
    /// deployment step (D-015), so start-up only checks that the schema is up to date and then seeds.
    /// <paramref name="bootstrapAdmin"/> creates the first administrator on a new environment, so
    /// someone can sign in and create the other accounts (FR-021).
    /// </summary>
    public async Task InitialiseAsync(
        string? demoPassword,
        bool applyMigrations = true,
        (string Email, string Password)? bootstrapAdmin = null,
        CancellationToken cancellationToken = default)
    {
        if (!db.Database.GetMigrations().Any())
            throw new InvalidOperationException(
                "No EF Core migrations exist yet. Create the first one with: " +
                "dotnet ef migrations add InitialCreate --project src/CivicConnect.Infrastructure " +
                "--startup-project src/CivicConnect.Web --output-dir Persistence/Migrations");

        if (applyMigrations)
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count > 0)
                throw new InvalidOperationException(
                    $"The database is missing {pending.Count} migration(s) ({string.Join(", ", pending)}). " +
                    "Run the migration bundle before starting the app (see docs/operations/deployment.md).");
        }

        foreach (var roleName in Enum.GetNames<Role>())
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await EnsureSucceeded(roleManager.CreateAsync(new IdentityRole<Guid>(roleName)), $"create role {roleName}");
        }

        if (!await db.Categories.AnyAsync(cancellationToken))
        {
            db.Categories.AddRange(InitialCategories.Select(c => new Category
            {
                Id = Guid.CreateVersion7(),
                Name = c.Name,
                IsActive = true,
                TargetResolutionDays = c.TargetDays
            }));
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} initial categories", InitialCategories.Length);
        }

        if (!string.IsNullOrEmpty(demoPassword))
            await SeedDemoUsersAsync(demoPassword);

        if (bootstrapAdmin is var (adminEmail, adminPassword) && await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new AppUser
            {
                Id = Guid.CreateVersion7(),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = "Administrator",
                IsActive = true,
                CreatedAt = time.GetUtcNow()
            };

            await EnsureSucceeded(userManager.CreateAsync(admin, adminPassword), $"create the first administrator {adminEmail}");
            await EnsureSucceeded(userManager.AddToRoleAsync(admin, nameof(Role.Administrator)), "give the first administrator the Administrator role");
            logger.LogInformation("Created the first administrator account {UserId}", admin.Id);
        }
    }

    private async Task SeedDemoUsersAsync(string password)
    {
        foreach (var (email, displayName, role) in DemoUsers)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                continue;

            var user = new AppUser
            {
                Id = Guid.CreateVersion7(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                IsActive = true,
                CreatedAt = time.GetUtcNow()
            };

            await EnsureSucceeded(userManager.CreateAsync(user, password), $"create demo user {email}");
            await EnsureSucceeded(userManager.AddToRoleAsync(user, role.ToString()), $"add {email} to {role}");
            logger.LogInformation("Created demo user {Email} with role {Role}", email, role);
        }
    }

    private static async Task EnsureSucceeded(Task<IdentityResult> operation, string description)
    {
        var result = await operation;
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Could not {description}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }
}
