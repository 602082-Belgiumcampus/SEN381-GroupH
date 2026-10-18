using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.RequestIntake;
using CivicConnect.Modules.RequestQueries;

namespace CivicConnect.Modules.Administration;

public sealed record UserSummary(
    Guid Id, string DisplayName, string Email, IReadOnlyList<Role> Roles, bool IsActive, bool HasPassword, DateTimeOffset CreatedAt);

public sealed record NewUserInput(string? DisplayName, string? Email, Role? Role, string? InitialPassword);

public sealed record CategorySummary(Guid Id, string Name, bool IsActive, int TargetResolutionDays, int RequestCount);

public sealed record CategoryInput(string? Name, int? TargetResolutionDays, bool IsActive = true);

/// <summary>The outcome of an administration command: success, or field errors to show on the form.</summary>
public sealed record AdminResult(IReadOnlyList<FieldError> Errors, Guid? Id = null)
{
    public bool Succeeded => Errors.Count == 0;

    public static AdminResult Ok(Guid? id = null) => new(Array.Empty<FieldError>(), id);

    public static AdminResult Fail(string field, string message) => new([new FieldError(field, message)]);

    public static AdminResult Fail(IReadOnlyList<FieldError> errors) => new(errors);
}

/// <summary>
/// Administration contract (FR-021, FR-022, CR-003). Only administrators reach these operations: the
/// /Admin pages require the Administrator role (TC-AZ-06). Users are deactivated, never deleted.
/// <paramref name="adminId"/> is the administrator making the change, so they cannot lock themselves out.
/// </summary>
public interface IAdministration
{
    Task<PagedResult<UserSummary>> GetUsersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<UserSummary?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<AdminResult> CreateUserAsync(NewUserInput input, CancellationToken cancellationToken = default);

    Task<AdminResult> SetRolesAsync(Guid adminId, Guid userId, IReadOnlyCollection<Role> roles, CancellationToken cancellationToken = default);

    Task<AdminResult> SetActiveAsync(Guid adminId, Guid userId, bool isActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategorySummary>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<AdminResult> CreateCategoryAsync(CategoryInput input, CancellationToken cancellationToken = default);

    Task<AdminResult> UpdateCategoryAsync(Guid categoryId, CategoryInput input, CancellationToken cancellationToken = default);
}

/// <summary>Administration rules that do not need the database.</summary>
public static class AdminRules
{
    public const int CategoryNameMaxLength = 100;
    public const int MaxTargetResolutionDays = 365;

    public static IReadOnlyList<FieldError> ValidateCategory(CategoryInput input)
    {
        var errors = new List<FieldError>();

        if (string.IsNullOrWhiteSpace(input.Name))
            errors.Add(new(nameof(CategoryInput.Name), "Enter a category name."));
        else if (input.Name.Trim().Length > CategoryNameMaxLength)
            errors.Add(new(nameof(CategoryInput.Name), $"Keep the name to {CategoryNameMaxLength} characters or fewer."));

        if (input.TargetResolutionDays is not int days || days < 1 || days > MaxTargetResolutionDays)
            errors.Add(new(nameof(CategoryInput.TargetResolutionDays), $"Enter a target between 1 and {MaxTargetResolutionDays} days."));

        return errors;
    }

    /// <summary>An administrator may not remove their own Administrator role or deactivate themselves.</summary>
    public static string? CheckSelfLockout(Guid adminId, Guid userId, IReadOnlyCollection<Role>? newRoles, bool? newActive)
    {
        if (adminId != userId)
            return null;
        if (newActive == false)
            return "You cannot deactivate your own account.";
        if (newRoles is not null && !newRoles.Contains(Role.Administrator))
            return "You cannot remove your own Administrator role.";
        return null;
    }
}
