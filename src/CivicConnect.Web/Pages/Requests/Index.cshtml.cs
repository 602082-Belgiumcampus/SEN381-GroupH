using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.RequestIntake;
using CivicConnect.Modules.RequestQueries;
using CivicConnect.Web.Pages.Shared;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Requests;

/// <summary>
/// "My requests" for the signed-in requester (FR-004, FR-005), with search, filter and sort (FR-008).
/// Only their own requests are queried.
/// </summary>
public class IndexModel(IRequestQueries queries, ISubmitRequest intake) : PageModel
{
    private const int PageSize = 20;

    // Not called "page": that route value is reserved by Razor Pages
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public RequestStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public RequestSort Sort { get; set; } = RequestSort.Newest;

    public RequestFilterForm Filters { get; private set; } = null!;

    public PagedResult<RequestSummary> Requests { get; private set; } =
        new(Array.Empty<RequestSummary>(), 1, PageSize, 0);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Filters = new RequestFilterForm(Search, Status, CategoryId, Sort, await intake.GetActiveCategoriesAsync(cancellationToken));
        Requests = await queries.GetMyRequestsAsync(User.GetUserId(), Filters.ToFilter(), PageNumber, PageSize, cancellationToken);
    }
}
