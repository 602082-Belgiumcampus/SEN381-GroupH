using CivicConnect.Modules.Reporting;
using CivicConnect.Modules.RequestIntake;
using CivicConnect.Modules.RequestQueries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Reports;

/// <summary>
/// Management reports (FR-014 to FR-017) for supervisors and management (CL-01). Read-only.
/// FR-014 volumes and outcomes, FR-015 overdue requests (CR-001), FR-016 filters,
/// FR-017 who did what, from the audit trail. No requester details are shown (AS-05).
/// </summary>
public class IndexModel(IReportQueries reports, ISubmitRequest intake) : PageModel
{
    private const int OverduePageSize = 25;

    [BindProperty(SupportsGet = true)]
    public DateOnly? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? To { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<CategoryOption> Categories { get; private set; } = Array.Empty<CategoryOption>();

    public ReportSummary Summary { get; private set; } = null!;

    public PagedResult<OverdueItem> Overdue { get; private set; } = null!;

    public IReadOnlyList<StaffActivityRow> Activity { get; private set; } = Array.Empty<StaffActivityRow>();

    public string? FilterError { get; private set; }

    public bool IsFiltered => From is not null || To is not null || CategoryId is not null;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        // FR-016: filter values come from the query string; an impossible range is reported, not run
        if (From is DateOnly from && To is DateOnly to && from > to)
        {
            FilterError = "The start date is after the end date, so the dates were ignored.";
            From = To = null;
        }

        var filter = new ReportFilter(From, To, CategoryId);
        Categories = await intake.GetActiveCategoriesAsync(cancellationToken);
        Summary = await reports.GetSummaryAsync(filter, cancellationToken);
        Overdue = await reports.GetOverdueAsync(filter, PageNumber, OverduePageSize, cancellationToken);
        Activity = await reports.GetStaffActivityAsync(filter, cancellationToken);
    }
}
