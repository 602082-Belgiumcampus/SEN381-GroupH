using System.ComponentModel.DataAnnotations;
using CivicConnect.Modules.RequestIntake;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CivicConnect.Web.Pages.Requests;

/// <summary>
/// Requester submit form (FR-001 to FR-003, NFR-009, NFR-011). The attributes give quick feedback
/// in the browser; <see cref="SubmitRequestRules"/> on the server is what actually decides.
/// </summary>
public class NewModel(ISubmitRequest intake) : PageModel
{
    public sealed class InputModel
    {
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

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        await LoadCategoriesAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        // Server-side rules run even if browser validation was bypassed (NFR-002, NFR-011)
        var result = await intake.SubmitAsync(
            User.GetUserId(),
            new SubmitRequestInput(Input.CategoryId, Input.Title, Input.Description, Input.Location),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.Clear();
            foreach (var error in result.Errors)
                ModelState.AddModelError($"{nameof(Input)}.{error.Field}", error.Message);

            await LoadCategoriesAsync(cancellationToken);
            return Page();
        }

        TempData["Flash"] = $"Your request has been submitted. Your reference is {result.Reference}.";
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
