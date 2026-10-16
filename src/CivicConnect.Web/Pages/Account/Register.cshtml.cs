using System.ComponentModel.DataAnnotations;
using CivicConnect.Infrastructure.Identity;
using CivicConnect.Modules.IdentityAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Account;

/// <summary>
/// Self-registration for requesters only. Staff, supervisor, management and administrator accounts
/// are created by an administrator (FR-021), never through this page.
/// </summary>
public class RegisterModel(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    TimeProvider time,
    ILogger<RegisterModel> logger) : PageModel
{
    public sealed class InputModel
    {
        [Required(ErrorMessage = "Enter your name.")]
        [StringLength(AppUser.DisplayNameMaxLength, ErrorMessage = "Keep your name to {1} characters or fewer.")]
        [Display(Name = "Your name")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose a password.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Use at least {2} characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the password again.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = new AppUser
        {
            Id = Guid.CreateVersion7(),
            UserName = Input.Email.Trim(),
            Email = Input.Email.Trim(),
            DisplayName = Input.DisplayName.Trim(),
            IsActive = true,
            CreatedAt = time.GetUtcNow()
        };

        var created = await userManager.CreateAsync(user, Input.Password);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        var roleAdded = await userManager.AddToRoleAsync(user, nameof(Role.Requester));
        if (!roleAdded.Succeeded)
            throw new InvalidOperationException(
                $"Could not give the new user the Requester role: {string.Join("; ", roleAdded.Errors.Select(e => e.Description))}");

        logger.LogInformation("Requester account {UserId} created", user.Id);
        await signInManager.SignInAsync(user, isPersistent: false);

        return !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? LocalRedirect(ReturnUrl)
            : RedirectToPage("/Requests/New");
    }
}
