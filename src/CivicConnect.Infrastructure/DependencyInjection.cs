using CivicConnect.Infrastructure.Identity;
using CivicConnect.Infrastructure.Lifecycle;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Infrastructure.RequestIntake;
using CivicConnect.Infrastructure.RequestQueries;
using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.RequestIntake;
using CivicConnect.Modules.RequestQueries;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CivicConnect.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "CivicConnect";

    /// <summary>
    /// Registers the database, ASP.NET Core Identity and the module services.
    /// The connection string is read from configuration (user-secrets or an environment variable),
    /// never from a committed file (NFR-003).
    /// </summary>
    public static IServiceCollection AddCivicConnectInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not set. For local development run: " +
                $"dotnet user-secrets set \"ConnectionStrings:{ConnectionStringName}\" \"Host=localhost;Port=5432;Database=civicconnect;Username=civicconnect;Password=<your password>\" --project src/CivicConnect.Web " +
                $"(or set the environment variable ConnectionStrings__{ConnectionStringName}).");

        services.AddDbContext<CivicConnectDbContext>(options => options.UseNpgsql(connectionString));

        services
            .AddIdentity<AppUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<CivicConnectDbContext>()
            .AddDefaultTokenProviders();

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ISubmitRequest, SubmitRequestService>();
        services.AddScoped<ILifecycleService, LifecycleService>();
        services.AddScoped<IRequestQueries, RequestQueryService>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
