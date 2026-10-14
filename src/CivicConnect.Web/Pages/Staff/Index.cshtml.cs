using CivicConnect.Modules.RequestQueries;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Staff;

/// <summary>
/// Staff work queue (FR-007). What each role sees follows AS-05: staff see their assigned requests
/// plus unassigned Submitted/Accepted ones; supervisors and management see everything.
/// </summary>
public class IndexModel(IRequestQueries queries) : PageModel
{
    private const int PageSize = 25;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<RequestSummary> Requests { get; private set; } =
        new(Array.Empty<RequestSummary>(), 1, PageSize, 0);

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Requests = await queries.GetStaffQueueAsync(User.ToActor(), PageNumber, PageSize, cancellationToken);
}
