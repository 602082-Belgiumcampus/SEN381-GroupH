using System.ComponentModel.DataAnnotations;
using CivicConnect.Modules.Administration;
using CivicConnect.Modules.IdentityAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Admin.Users;

/// <summary>FR-021: create an account with one role and an initial password. Administrators only.</summary>
public class NewModel(IAdministration admin) : PageModel
{
    public sealed class InputModel
    {
        [Required(ErrorMessage = "Enter the person's name.")]
        [Display(Name = "Name")]
        public string? DisplayName { get; set; }

        [Required(ErrorMessage = "Enter an email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email address")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Choose a role.")]
        [Display(Name = "Role")]
        public Role? Role { get; set; }

        [Required(ErrorMessage = "Enter an initial password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Initial password")]
        public string? InitialPassword { get; set; }
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await admin.CreateUserAsync(
            new NewUserInput(Input.DisplayName, Input.Email, Input.Role, Input.InitialPassword), cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.IsNullOrEmpty(error.Field) ? string.Empty : $"{nameof(Input)}.{error.Field}", error.Message);
            return Page();
        }

        TempData["Flash"] = $"Account created for {Input.Email!.Trim()}. Give them the initial password in person and ask them to keep it private.";
        return RedirectToPage("/Admin/Users/Index");
    }
}
