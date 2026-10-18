using CivicConnect.Modules.Administration;
using CivicConnect.Modules.RequestQueries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Admin.Users;

/// <summary>FR-021: list and search user accounts. Administrators only (folder policy).</summary>
public class IndexModel(IAdministration admin) : PageModel
{
    private const int PageSize = 25;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<UserSummary> Users { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Users = await admin.GetUsersAsync(Search, PageNumber, PageSize, cancellationToken);
}
