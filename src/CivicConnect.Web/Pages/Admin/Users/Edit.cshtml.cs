using CivicConnect.Modules.Administration;
using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Admin.Users;

/// <summary>
/// FR-021: change a user's roles, or deactivate / reactivate them (AC-038). Administrators only.
/// The change signs the user out of existing sessions within a minute.
/// </summary>
public class EditModel(IAdministration admin) : PageModel
{
    public UserSummary UserAccount { get; private set; } = null!;

    [BindProperty]
    public List<Role> Roles { get; set; } = [];

    public bool IsSelf => UserAccount.Id == User.GetUserId();

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
            return NotFound();

        Roles = UserAccount.Roles.ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostRolesAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await admin.SetRolesAsync(User.GetUserId(), id, Roles.Distinct().ToList(), cancellationToken);
        return await AfterAsync(id, result, "Roles saved. The user must sign in again for them to apply.", cancellationToken);
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await admin.SetActiveAsync(User.GetUserId(), id, isActive: false, cancellationToken);
        return await AfterAsync(id, result, "Account deactivated. The user is signed out and can no longer sign in.", cancellationToken);
    }

    public async Task<IActionResult> OnPostReactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await admin.SetActiveAsync(User.GetUserId(), id, isActive: true, cancellationToken);
        return await AfterAsync(id, result, "Account reactivated. The user can sign in again.", cancellationToken);
    }

    private async Task<IActionResult> AfterAsync(Guid id, AdminResult result, string successMessage, CancellationToken cancellationToken)
    {
        if (result.Succeeded)
        {
            TempData["Flash"] = successMessage;
            return RedirectToPage(new { id });
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(error.Field, error.Message);

        return await LoadAsync(id, cancellationToken) ? Page() : NotFound();
    }

    private async Task<bool> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await admin.GetUserAsync(id, cancellationToken);
        if (user is null)
            return false;

        UserAccount = user;
        return true;
    }
}
