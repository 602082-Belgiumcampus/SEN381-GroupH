using System.ComponentModel.DataAnnotations;
using CivicConnect.Modules.RequestIntake;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CivicConnect.Web.Pages.Staff;

/// <summary>
/// FR-020: staff log a request for someone who phoned, emailed or walked in. The requester is found
/// by email, or an account is created for them (OD-07). The service decides; the attributes here only
/// give quick feedback in the browser.
/// </summary>
public class LogRequestModel(ISubmitRequest intake) : PageModel
{
    public sealed class InputModel
    {
        [Required(ErrorMessage = "Enter the requester's email address.")]
        [Display(Name = "Requester's email")]
        public string? RequesterEmail { get; set; }

        [StringLength(OnBehalfRules.NameMaxLength, ErrorMessage = "Keep the name to {1} characters or fewer.")]
        [Display(Name = "Requester's name (only if they have no account yet)")]
        public string? RequesterName { get; set; }

        [Required(ErrorMessage = "Choose how the requester contacted you.")]
        [Display(Name = "How they contacted you")]
        public string? ContactChannel { get; set; }

        [Required(ErrorMessage = "Choose a category.")]
        [Display(Name = "Category")]
        public Guid? CategoryId { get; set; }

        [Required(ErrorMessage = "Enter a title.")]
        [StringLength(SubmitRequestRules.TitleMaxLength, ErrorMessage = "Keep the title to {1} characters or fewer.")]
        [Display(Name = "Title")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Enter a description.")]
        [StringLength(SubmitRequestRules.DescriptionMaxLength, ErrorMessage = "Keep the description to {1} characters or fewer.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(SubmitRequestRules.LocationMaxLength, ErrorMessage = "Keep the location to {1} characters or fewer.")]
        [Display(Name = "Location (optional)")]
        public string? Location { get; set; }
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<SelectListItem> Categories { get; private set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> Channels =>
        ContactChannels.ForStaff.Select(c => new SelectListItem(c, c, c == Input.ContactChannel)).ToList();

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        await LoadCategoriesAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var result = await intake.SubmitOnBehalfAsync(
            User.ToActor(),
            new OnBehalfInput(Input.RequesterEmail, Input.RequesterName, Input.ContactChannel,
                new SubmitRequestInput(Input.CategoryId, Input.Title, Input.Description, Input.Location)),
            cancellationToken);

        if (result.IsForbidden)
            return Forbid();

        if (!result.Succeeded)
        {
            ModelState.Clear();
            foreach (var error in result.Errors)
                ModelState.AddModelError($"{nameof(Input)}.{error.Field}", error.Message);

            await LoadCategoriesAsync(cancellationToken);
            return Page();
        }

        TempData["Flash"] = $"Request {result.Reference} logged for {Input.RequesterEmail!.Trim()}. Give them this reference.";
        return RedirectToPage("/Requests/Details", new { id = result.RequestId });
    }

    private async Task LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await intake.GetActiveCategoriesAsync(cancellationToken);
        Categories = categories
            .Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == Input.CategoryId))
            .ToList();
    }
}
