using System.ComponentModel.DataAnnotations;
using CivicConnect.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Account;

public class LoginModel(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, ILogger<LoginModel> logger)
    : PageModel
{
    // One message for every failed sign-in, so the page does not reveal which accounts exist
    private const string FailedMessage = "The email address or password is incorrect.";

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Keep me signed in on this device")]
        public bool RememberMe { get; set; }
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(SafeReturnUrl());
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await userManager.FindByEmailAsync(Input.Email);

        // Deactivated users can no longer sign in (FR-021, AC-038)
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, FailedMessage);
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation("User {UserId} signed in", user.Id);
            return LocalRedirect(SafeReturnUrl());
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User {UserId} is locked out after repeated failed sign-ins", user.Id);
            ModelState.AddModelError(string.Empty, "Too many failed attempts. Wait a few minutes and try again.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, FailedMessage);
        return Page();
    }

    private string SafeReturnUrl() =>
        !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : Url.Content("~/");
}
