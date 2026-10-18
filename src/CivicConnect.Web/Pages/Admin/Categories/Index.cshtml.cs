using System.ComponentModel.DataAnnotations;
using CivicConnect.Modules.Administration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Admin.Categories;

/// <summary>FR-022: the controlled category list, with each category's target resolution period (CR-001).</summary>
public class IndexModel(IAdministration admin) : PageModel
{
    public sealed class InputModel
    {
        [Required(ErrorMessage = "Enter a category name.")]
        [StringLength(AdminRules.CategoryNameMaxLength, ErrorMessage = "Keep the name to {1} characters or fewer.")]
        [Display(Name = "Name")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Enter a target in days.")]
        [Range(1, AdminRules.MaxTargetResolutionDays, ErrorMessage = "Enter a target between {1} and {2} days.")]
        [Display(Name = "Target resolution (days)")]
        public int? TargetResolutionDays { get; set; }
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<CategorySummary> Categories { get; private set; } = Array.Empty<CategorySummary>();

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Categories = await admin.GetCategoriesAsync(cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await admin.CreateCategoryAsync(new CategoryInput(Input.Name, Input.TargetResolutionDays), cancellationToken);
            if (result.Succeeded)
            {
                TempData["Flash"] = $"Category \"{Input.Name!.Trim()}\" added. It now appears on the submit form.";
                return RedirectToPage();
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.IsNullOrEmpty(error.Field) ? string.Empty : $"{nameof(Input)}.{error.Field}", error.Message);
        }

        Categories = await admin.GetCategoriesAsync(cancellationToken);
        return Page();
    }
}
