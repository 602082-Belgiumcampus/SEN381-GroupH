using CivicConnect.Modules.RequestQueries;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Requests;

/// <summary>"My requests" for the signed-in requester (FR-004, FR-005). Only their own requests are queried.</summary>
public class IndexModel(IRequestQueries queries) : PageModel
{
    private const int PageSize = 20;

    // Not called "page": that route value is reserved by Razor Pages
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<RequestSummary> Requests { get; private set; } =
        new(Array.Empty<RequestSummary>(), 1, PageSize, 0);

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Requests = await queries.GetMyRequestsAsync(User.GetUserId(), PageNumber, PageSize, cancellationToken);
}
