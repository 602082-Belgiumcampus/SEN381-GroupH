using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.RequestIntake;
using CivicConnect.Modules.RequestQueries;
using CivicConnect.Web.Pages.Shared;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Staff;

/// <summary>
/// Staff work queue (FR-007) with search, filter and sort (FR-008). What each role sees follows AS-05:
/// staff see their assigned requests plus unassigned Submitted/Accepted ones; supervisors and
/// management see everything. Search and filters only ever narrow that.
/// </summary>
public class IndexModel(IRequestQueries queries, ISubmitRequest intake) : PageModel
{
    private const int PageSize = 25;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public RequestStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public RequestSort Sort { get; set; } = RequestSort.Oldest;

    public RequestFilterForm Filters { get; private set; } = null!;

    public PagedResult<RequestSummary> Requests { get; private set; } =
        new(Array.Empty<RequestSummary>(), 1, PageSize, 0);

    public bool CanLogOnBehalf => OnBehalfRules.CanLogOnBehalf(User.ToActor());

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Filters = new RequestFilterForm(Search, Status, CategoryId, Sort, await intake.GetActiveCategoriesAsync(cancellationToken));
        Requests = await queries.GetStaffQueueAsync(User.ToActor(), Filters.ToFilter(), PageNumber, PageSize, cancellationToken);
    }
}
