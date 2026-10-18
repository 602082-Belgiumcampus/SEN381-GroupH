using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.RequestQueries;

namespace CivicConnect.Modules.Reporting;

/// <summary>FR-016: which requests a report covers. Dates are on the request's creation date, inclusive.</summary>
public sealed record ReportFilter(DateOnly? From = null, DateOnly? To = null, Guid? CategoryId = null);

/// <summary>One request as the reports see it. No requester details (AS-05).</summary>
public sealed record ReportRow(
    RequestStatus Status, string Category, int TargetResolutionDays, DateTimeOffset CreatedAt, DateTimeOffset? ResolvedAt);

public sealed record StatusCount(RequestStatus Status, int Count);

public sealed record CategoryReportRow(
    string Category, int TargetResolutionDays, int Total, int Open, int Overdue, double? AverageDaysToResolve);

/// <summary>FR-014: request volumes and outcomes; FR-015: how many are overdue.</summary>
public sealed record ReportSummary(
    int Total,
    int Open,
    int Overdue,
    double? AverageDaysToResolve,
    IReadOnlyList<StatusCount> ByStatus,
    IReadOnlyList<CategoryReportRow> ByCategory);

/// <summary>FR-015: an overdue request. Shows who it is assigned to, never who reported it.</summary>
public sealed record OverdueItem(
    Guid Id, string Reference, string Title, string Category, RequestStatus Status, string? AssignedTo,
    DateTimeOffset CreatedAt, DateTimeOffset DueAt, int DaysOverdue);

/// <summary>FR-017: what each staff member did in the period, from the audit trail.</summary>
public sealed record StaffActivityRow(
    string StaffName, int Accepted, int Rejected, int Assigned, int Started, int Resolved, int Closed,
    int Reopened, int ActionsRecorded, int LoggedOnBehalf)
{
    public int Total => Accepted + Rejected + Assigned + Started + Resolved + Closed + Reopened + ActionsRecorded + LoggedOnBehalf;
}

/// <summary>Reporting contract (ADR-008): read-only, never writes (FR-014 to FR-017).</summary>
public interface IReportQueries
{
    Task<ReportSummary> GetSummaryAsync(ReportFilter filter, CancellationToken cancellationToken = default);

    Task<PagedResult<OverdueItem>> GetOverdueAsync(ReportFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffActivityRow>> GetStaffActivityAsync(ReportFilter filter, CancellationToken cancellationToken = default);
}

/// <summary>The report arithmetic, kept pure so it can be tested without a database.</summary>
public static class ReportCalculator
{
    public static ReportSummary Summarise(IReadOnlyCollection<ReportRow> rows, DateTimeOffset now)
    {
        var byStatus = Enum.GetValues<RequestStatus>()
            .Select(status => new StatusCount(status, rows.Count(r => r.Status == status)))
            .ToList();

        var byCategory = rows
            .GroupBy(r => (r.Category, r.TargetResolutionDays))
            .OrderBy(g => g.Key.Category)
            .Select(g => new CategoryReportRow(
                g.Key.Category,
                g.Key.TargetResolutionDays,
                g.Count(),
                g.Count(r => OverdueRule.IsOpen(r.Status)),
                g.Count(r => IsOverdue(r, now)),
                AverageDaysToResolve(g)))
            .ToList();

        return new ReportSummary(
            rows.Count,
            rows.Count(r => OverdueRule.IsOpen(r.Status)),
            rows.Count(r => IsOverdue(r, now)),
            AverageDaysToResolve(rows),
            byStatus,
            byCategory);
    }

    public static bool IsOverdue(ReportRow row, DateTimeOffset now) =>
        OverdueRule.IsOverdue(row.Status, row.CreatedAt, row.TargetResolutionDays, now);

    /// <summary>Whole days late, counting a part day as a full day (1.2 days late shows as 2).</summary>
    public static int DaysOverdue(DateTimeOffset dueAt, DateTimeOffset now) =>
        now <= dueAt ? 0 : (int)Math.Ceiling((now - dueAt).TotalDays);

    private static double? AverageDaysToResolve(IEnumerable<ReportRow> rows)
    {
        var durations = rows
            .Where(r => r.ResolvedAt is not null)
            .Select(r => (r.ResolvedAt!.Value - r.CreatedAt).TotalDays)
            .ToList();

        return durations.Count == 0 ? null : Math.Round(durations.Average(), 1);
    }
}
