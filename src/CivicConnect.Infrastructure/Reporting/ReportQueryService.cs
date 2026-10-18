using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.Reporting;
using CivicConnect.Modules.RequestQueries;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Reporting;

/// <summary>
/// Reporting (FR-014 to FR-017). Read-only: no-tracking queries, never SaveChanges. Requester
/// details are never selected (AS-05). The arithmetic lives in <see cref="ReportCalculator"/>.
/// </summary>
public sealed class ReportQueryService(CivicConnectDbContext db, TimeProvider time) : IReportQueries
{
    public const int MaxPageSize = 100;

    public async Task<ReportSummary> GetSummaryAsync(ReportFilter filter, CancellationToken cancellationToken = default)
    {
        var rows = await Requests(filter)
            .Select(r => new
            {
                r.Status,
                Category = r.Category.Name,
                r.Category.TargetResolutionDays,
                r.CreatedAt,
                // First time the request was resolved, from the audit trail
                ResolvedAt = db.AuditEntries
                    .Where(a => a.RequestId == r.Id && a.EventType == nameof(LifecycleEvent.Resolve))
                    .Min(a => (DateTimeOffset?)a.OccurredAt)
            })
            .ToListAsync(cancellationToken);

        return ReportCalculator.Summarise(
            rows.Select(r => new ReportRow(r.Status, r.Category, r.TargetResolutionDays, r.CreatedAt, r.ResolvedAt)).ToList(),
            time.GetUtcNow());
    }

    public async Task<PagedResult<OverdueItem>> GetOverdueAsync(
        ReportFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        // Open requests are few compared with all requests, so the overdue rule runs in memory
        var open = await Requests(filter)
            .Where(r => r.Status != RequestStatus.Resolved && r.Status != RequestStatus.Closed && r.Status != RequestStatus.Rejected)
            .Select(r => new
            {
                r.Id, r.Reference, r.Title, Category = r.Category.Name, r.Status,
                AssignedTo = r.AssignedTo == null ? null : r.AssignedTo.DisplayName,
                r.CreatedAt, r.Category.TargetResolutionDays
            })
            .ToListAsync(cancellationToken);

        var now = time.GetUtcNow();
        var overdue = open
            .Where(r => OverdueRule.IsOverdue(r.Status, r.CreatedAt, r.TargetResolutionDays, now))
            .Select(r =>
            {
                var dueAt = OverdueRule.DueAt(r.CreatedAt, r.TargetResolutionDays);
                return new OverdueItem(r.Id, r.Reference, r.Title, r.Category, r.Status, r.AssignedTo,
                    r.CreatedAt, dueAt, ReportCalculator.DaysOverdue(dueAt, now));
            })
            .OrderByDescending(r => r.DaysOverdue).ThenBy(r => r.Reference)
            .ToList();

        var items = overdue.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<OverdueItem>(items, page, pageSize, overdue.Count);
    }

    public async Task<IReadOnlyList<StaffActivityRow>> GetStaffActivityAsync(
        ReportFilter filter, CancellationToken cancellationToken = default)
    {
        // FR-017 counts what people did in the period, so the dates apply to the audit entry, not the request
        var entries = db.AuditEntries.AsNoTracking().AsQueryable();
        if (Start(filter) is DateTimeOffset from)
            entries = entries.Where(a => a.OccurredAt >= from);
        if (EndExclusive(filter) is DateTimeOffset to)
            entries = entries.Where(a => a.OccurredAt < to);
        if (filter.CategoryId is Guid categoryId)
            entries = entries.Where(a => a.Request.CategoryId == categoryId);

        // Requesters' own "Submit" entries are not staff activity
        var counts = await entries
            .Where(a => a.EventType != "Submit")
            .GroupBy(a => new { a.ActorId, a.Actor.DisplayName, a.EventType })
            .Select(g => new { g.Key.ActorId, g.Key.DisplayName, g.Key.EventType, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts
            .GroupBy(c => (c.ActorId, c.DisplayName))
            .Select(g =>
            {
                int Count(string eventType) => g.Where(c => c.EventType == eventType).Sum(c => c.Count);
                return new StaffActivityRow(
                    g.Key.DisplayName,
                    Count(nameof(LifecycleEvent.Accept)), Count(nameof(LifecycleEvent.Reject)),
                    Count(nameof(LifecycleEvent.Assign)), Count(nameof(LifecycleEvent.Start)),
                    Count(nameof(LifecycleEvent.Resolve)), Count(nameof(LifecycleEvent.Close)),
                    Count(nameof(LifecycleEvent.Reopen)), Count("RecordAction"), Count("LoggedOnBehalf"));
            })
            .OrderByDescending(r => r.Total).ThenBy(r => r.StaffName)
            .ToList();
    }

    /// <summary>FR-016: the requests a report covers, by creation date and category.</summary>
    private IQueryable<ServiceRequest> Requests(ReportFilter filter)
    {
        var requests = db.ServiceRequests.AsNoTracking().AsQueryable();
        if (Start(filter) is DateTimeOffset from)
            requests = requests.Where(r => r.CreatedAt >= from);
        if (EndExclusive(filter) is DateTimeOffset to)
            requests = requests.Where(r => r.CreatedAt < to);
        if (filter.CategoryId is Guid categoryId)
            requests = requests.Where(r => r.CategoryId == categoryId);
        return requests;
    }

    // Report dates are whole days in UTC; "To" includes the whole of that day
    private static DateTimeOffset? Start(ReportFilter filter) =>
        filter.From is DateOnly from ? new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;

    private static DateTimeOffset? EndExclusive(ReportFilter filter) =>
        filter.To is DateOnly to ? new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;
}
