using CivicConnect.Infrastructure.Identity;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Modules.Administration;
using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.RequestQueries;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CivicConnect.Infrastructure.Administration;

/// <summary>
/// Administration (FR-021, FR-022). User accounts go through ASP.NET Core Identity so passwords stay
/// hashed (NFR-001). Changing someone's roles or deactivating them refreshes their security stamp,
/// which signs them out of existing sessions (AC-038). Admin changes are logged, not yet audited (FE-09).
/// </summary>
public sealed class AdministrationService(
    CivicConnectDbContext db,
    UserManager<AppUser> users,
    TimeProvider time,
    ILogger<AdministrationService> logger) : IAdministration
{
    public const int MaxPageSize = 100;

    public async Task<PagedResult<UserSummary>> GetUsersAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = db.Users.AsNoTracking();
        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var pattern = "%" + term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(u => EF.Functions.ILike(u.DisplayName, pattern, "\\") || EF.Functions.ILike(u.Email!, pattern, "\\"));
        }

        var total = await query.CountAsync(cancellationToken);
        var pageOfUsers = await query
            .OrderBy(u => u.DisplayName).ThenBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.Id, u.DisplayName, u.Email, u.IsActive, HasPassword = u.PasswordHash != null, u.CreatedAt })
            .ToListAsync(cancellationToken);

        var ids = pageOfUsers.Select(u => u.Id).ToList();
        var roles = await RolesForAsync(ids, cancellationToken);

        var items = pageOfUsers
            .Select(u => new UserSummary(u.Id, u.DisplayName, u.Email ?? string.Empty,
                roles.GetValueOrDefault(u.Id, []), u.IsActive, u.HasPassword, u.CreatedAt))
            .ToList();

        return new PagedResult<UserSummary>(items, page, pageSize, total);
    }

    public async Task<UserSummary?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return null;

        var roles = await RolesForAsync([userId], cancellationToken);
        return new UserSummary(user.Id, user.DisplayName, user.Email ?? string.Empty,
            roles.GetValueOrDefault(user.Id, []), user.IsActive, user.PasswordHash is not null, user.CreatedAt);
    }

    public async Task<AdminResult> CreateUserAsync(NewUserInput input, CancellationToken cancellationToken = default)
    {
        var errors = new List<Modules.RequestIntake.FieldError>();
        if (string.IsNullOrWhiteSpace(input.DisplayName))
            errors.Add(new(nameof(NewUserInput.DisplayName), "Enter the person's name."));
        else if (input.DisplayName.Trim().Length > AppUser.DisplayNameMaxLength)
            errors.Add(new(nameof(NewUserInput.DisplayName), $"Keep the name to {AppUser.DisplayNameMaxLength} characters or fewer."));
        if (string.IsNullOrWhiteSpace(input.Email))
            errors.Add(new(nameof(NewUserInput.Email), "Enter an email address."));
        if (input.Role is null)
            errors.Add(new(nameof(NewUserInput.Role), "Choose a role."));
        if (string.IsNullOrEmpty(input.InitialPassword))
            errors.Add(new(nameof(NewUserInput.InitialPassword), "Enter an initial password."));
        if (errors.Count > 0)
            return AdminResult.Fail(errors);

        var email = input.Email!.Trim();
        var user = new AppUser
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = input.DisplayName!.Trim(),
            IsActive = true,
            CreatedAt = time.GetUtcNow()
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var created = await users.CreateAsync(user, input.InitialPassword!);
        if (!created.Succeeded)
            return AdminResult.Fail(created.Errors.Select(e => new Modules.RequestIntake.FieldError(FieldFor(e), e.Description)).ToList());

        var roleAdded = await users.AddToRoleAsync(user, input.Role!.Value.ToString());
        if (!roleAdded.Succeeded)
            return AdminResult.Fail(nameof(NewUserInput.Role), string.Join(" ", roleAdded.Errors.Select(e => e.Description)));

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Administrator created user {UserId} with role {Role}", user.Id, input.Role);
        return AdminResult.Ok(user.Id);
    }

    public async Task<AdminResult> SetRolesAsync(
        Guid adminId, Guid userId, IReadOnlyCollection<Role> roles, CancellationToken cancellationToken = default)
    {
        if (roles.Count == 0)
            return AdminResult.Fail("Roles", "Choose at least one role.");
        if (AdminRules.CheckSelfLockout(adminId, userId, roles, newActive: null) is string lockout)
            return AdminResult.Fail("Roles", lockout);

        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
            return AdminResult.Fail(string.Empty, "That user no longer exists.");

        var wanted = roles.Select(r => r.ToString()).ToHashSet();
        var current = (await users.GetRolesAsync(user)).ToHashSet();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var removed = await users.RemoveFromRolesAsync(user, current.Except(wanted));
        var added = await users.AddToRolesAsync(user, wanted.Except(current));
        if (!removed.Succeeded || !added.Succeeded)
            return AdminResult.Fail("Roles", "The roles could not be changed. Try again.");

        // Signs the user out of existing sessions so the new roles apply from their next sign-in
        await users.UpdateSecurityStampAsync(user);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Administrator {AdminId} set roles of user {UserId} to {Roles}", adminId, userId, string.Join(",", wanted));
        return AdminResult.Ok(userId);
    }

    public async Task<AdminResult> SetActiveAsync(Guid adminId, Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (AdminRules.CheckSelfLockout(adminId, userId, newRoles: null, isActive) is string lockout)
            return AdminResult.Fail(string.Empty, lockout);

        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
            return AdminResult.Fail(string.Empty, "That user no longer exists.");

        user.IsActive = isActive;
        var updated = await users.UpdateAsync(user);
        if (!updated.Succeeded)
            return AdminResult.Fail(string.Empty, "The account could not be updated. Try again.");

        // A deactivated user is signed out of existing sessions as well as refused at sign-in (AC-038)
        await users.UpdateSecurityStampAsync(user);

        logger.LogInformation("Administrator {AdminId} {Action} user {UserId}", adminId, isActive ? "reactivated" : "deactivated", userId);
        return AdminResult.Ok(userId);
    }

    public async Task<IReadOnlyList<CategorySummary>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await db.Categories
            .AsNoTracking()
            .OrderByDescending(c => c.IsActive).ThenBy(c => c.Name)
            .Select(c => new CategorySummary(c.Id, c.Name, c.IsActive, c.TargetResolutionDays,
                db.ServiceRequests.Count(r => r.CategoryId == c.Id)))
            .ToListAsync(cancellationToken);

    public async Task<AdminResult> CreateCategoryAsync(CategoryInput input, CancellationToken cancellationToken = default)
    {
        var errors = AdminRules.ValidateCategory(input);
        if (errors.Count > 0)
            return AdminResult.Fail(errors);

        var name = input.Name!.Trim();
        if (await NameTakenAsync(name, exceptId: null, cancellationToken))
            return AdminResult.Fail(nameof(CategoryInput.Name), "A category with that name already exists.");

        var category = new Category
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            IsActive = input.IsActive,
            TargetResolutionDays = input.TargetResolutionDays!.Value
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Administrator created category {CategoryId}", category.Id);
        return AdminResult.Ok(category.Id);
    }

    public async Task<AdminResult> UpdateCategoryAsync(Guid categoryId, CategoryInput input, CancellationToken cancellationToken = default)
    {
        var errors = AdminRules.ValidateCategory(input);
        if (errors.Count > 0)
            return AdminResult.Fail(errors);

        var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
        if (category is null)
            return AdminResult.Fail(string.Empty, "That category no longer exists.");

        var name = input.Name!.Trim();
        if (await NameTakenAsync(name, categoryId, cancellationToken))
            return AdminResult.Fail(nameof(CategoryInput.Name), "A category with that name already exists.");

        // Existing requests keep their category; an inactive one just disappears from the submit form (AC-039)
        category.Name = name;
        category.TargetResolutionDays = input.TargetResolutionDays!.Value;
        category.IsActive = input.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Administrator updated category {CategoryId}", categoryId);
        return AdminResult.Ok(categoryId);
    }

    private Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken cancellationToken) =>
        db.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower() && c.Id != exceptId, cancellationToken);

    private async Task<Dictionary<Guid, IReadOnlyList<Role>>> RolesForAsync(List<Guid> userIds, CancellationToken cancellationToken)
    {
        var pairs = await (from userRole in db.UserRoles
                           join role in db.Roles on userRole.RoleId equals role.Id
                           where userIds.Contains(userRole.UserId)
                           select new { userRole.UserId, role.Name })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return pairs
            .GroupBy(p => p.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<Role>)g
                    .Select(p => Enum.TryParse<Role>(p.Name, out var role) ? role : (Role?)null)
                    .OfType<Role>()
                    .Order()
                    .ToList());
    }

    private static string FieldFor(IdentityError error) =>
        error.Code switch
        {
            "DuplicateEmail" or "DuplicateUserName" or "InvalidEmail" or "InvalidUserName" => nameof(NewUserInput.Email),
            var code when code.StartsWith("Password", StringComparison.Ordinal) => nameof(NewUserInput.InitialPassword),
            _ => string.Empty
        };
}
