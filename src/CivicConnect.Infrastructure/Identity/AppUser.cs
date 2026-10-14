using Microsoft.AspNetCore.Identity;

namespace CivicConnect.Infrastructure.Identity;

/// <summary>
/// A person who signs in (PED Section 6.2, AppUser). Credentials are stored by ASP.NET Core Identity
/// as salted hashes (NFR-001). Users are deactivated, never deleted (FR-021).
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public const int DisplayNameMaxLength = 100;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
}
