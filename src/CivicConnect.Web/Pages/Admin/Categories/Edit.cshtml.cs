using System.ComponentModel.DataAnnotations;
using CivicConnect.Modules.Administration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Admin.Categories;

/// <summary>
/// FR-022: rename a category, change its target period, or deactivate it (AC-039). Deactivated
/// categories disappear from the submit form; existing requests keep them.
/// </summary>
public class EditModel(IAdministration admin) : PageModel
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

        [Display(Name = "Active (shown on the submit form)")]
        public bool IsActive { get; set; }
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public CategorySummary Category { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
            return NotFound();

        Input = new InputModel { Name = Category.Name, TargetResolutionDays = Category.TargetResolutionDays, IsActive = Category.IsActive };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
            return NotFound();

        if (!ModelState.IsValid)
            return Page();

        var result = await admin.UpdateCategoryAsync(id, new CategoryInput(Input.Name, Input.TargetResolutionDays, Input.IsActive), cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.IsNullOrEmpty(error.Field) ? string.Empty : $"{nameof(Input)}.{error.Field}", error.Message);
            return Page();
        }

        TempData["Flash"] = $"Category \"{Input.Name!.Trim()}\" saved.";
        return RedirectToPage("/Admin/Categories/Index");
    }

    private async Task<bool> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = (await admin.GetCategoriesAsync(cancellationToken)).SingleOrDefault(c => c.Id == id);
        if (category is null)
            return false;

        Category = category;
        return true;
    }
}
