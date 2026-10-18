using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.Reporting;
using CivicConnect.Modules.RequestQueries;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.RequestQueries;

/// <summary>
/// Request Queries (FR-004, FR-005, FR-007, FR-008, FR-009). Read-only and paged (NFR-005).
/// The staff queue filter is the database form of <see cref="RequestVisibility"/>;
/// the detail query applies RequestVisibility itself.
/// </summary>
public sealed class RequestQueryService(CivicConnectDbContext db, TimeProvider time) : IRequestQueries
{
    public const int MaxPageSize = 100;

    public Task<PagedResult<RequestSummary>> GetMyRequestsAsync(
        Guid requesterId, RequestFilter filter, int page, int pageSize, CancellationToken cancellationToken = default) =>
        PageAsync(
            Apply(db.ServiceRequests.Where(r => r.RequesterId == requesterId), filter),
            page, pageSize, cancellationToken);

    public Task<PagedResult<RequestSummary>> GetStaffQueueAsync(
        Actor actor, RequestFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var requests = db.ServiceRequests.AsQueryable();

        if (actor.IsInRole(Role.Supervisor) || actor.IsInRole(Role.Management))
        {
            // All requests (AS-05)
        }
        else if (actor.IsInRole(Role.ServiceStaff))
        {
            var userId = actor.UserId;
            requests = requests.Where(r =>
                r.AssignedToId == userId
                || (r.AssignedToId == null && (r.Status == RequestStatus.Submitted || r.Status == RequestStatus.Accepted)));
        }
        else
        {
            requests = requests.Where(_ => false);
        }

        // Visibility is applied first, so search and filters can only narrow what the user may see
        return PageAsync(Apply(requests, filter), page, pageSize, cancellationToken);
    }

    /// <summary>FR-008: search, filter and sort. Values come from the query string and are treated as data only.</summary>
    private static IQueryable<ServiceRequest> Apply(IQueryable<ServiceRequest> requests, RequestFilter filter)
    {
        var search = filter.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            if (search.Length > RequestFilter.SearchMaxLength)
                search = search[..RequestFilter.SearchMaxLength];

            // Escape LIKE wildcards so "%" or "_" typed by the user match literally
            var pattern = "%" + search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            requests = requests.Where(r =>
                EF.Functions.ILike(r.Reference, pattern, "\\") || EF.Functions.ILike(r.Title, pattern, "\\"));
        }

        if (filter.Status is RequestStatus status)
            requests = requests.Where(r => r.Status == status);

        if (filter.CategoryId is Guid categoryId)
            requests = requests.Where(r => r.CategoryId == categoryId);

        // Id as the final tie-break keeps paging stable when two requests share a timestamp
        return filter.Sort switch
        {
            RequestSort.Newest => requests.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id),
            RequestSort.RecentlyUpdated => requests.OrderByDescending(r => r.UpdatedAt).ThenByDescending(r => r.Id),
            _ => requests.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
        };
    }

    public async Task<RequestDetail?> GetDetailAsync(Guid requestId, Actor actor, CancellationToken cancellationToken = default)
    {
        var request = await db.ServiceRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(r => new
            {
                r.Id, r.Reference, r.Title, r.Description, r.Location,
                Category = r.Category.Name,
                r.Status, r.Version,
                r.RequesterId,
                RequesterName = r.Requester.DisplayName,
                r.AssignedToId,
                AssignedToName = r.AssignedTo == null ? null : r.AssignedTo.DisplayName,
                r.RejectionReason, r.ResolutionNote,
                r.CreatedAt, r.UpdatedAt, r.ClosedAt,
                r.ContactChannel,
                LoggedByName = r.LoggedBy == null ? null : r.LoggedBy.DisplayName
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (request is null
            || !RequestVisibility.CanView(actor, new RequestAccessFacts(request.RequesterId, request.AssignedToId, request.Status)))
            return null;

        var history = await db.AuditEntries
            .AsNoTracking()
            .Where(a => a.RequestId == requestId)
            .OrderBy(a => a.OccurredAt).ThenBy(a => a.Id)
            .Select(a => new HistoryItem(a.EventType, a.FromStatus, a.ToStatus, a.Actor.DisplayName, a.Detail, a.OccurredAt))
            .ToListAsync(cancellationToken);

        var workLog = await db.RequestActions
            .AsNoTracking()
            .Where(a => a.RequestId == requestId)
            .OrderBy(a => a.CreatedAt).ThenBy(a => a.Id)
            .Select(a => new WorkLogItem(a.Text, a.Author.DisplayName, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return new RequestDetail(
            request.Id, request.Reference, request.Title, request.Description, request.Location,
            request.Category, request.Status, request.Version,
            request.RequesterId, request.RequesterName,
            request.AssignedToId, request.AssignedToName,
            request.RejectionReason, request.ResolutionNote,
            request.CreatedAt, request.UpdatedAt, request.ClosedAt,
            history, workLog, request.ContactChannel, request.LoggedByName);
    }

    public async Task<IReadOnlyList<StaffOption>> GetAssignableStaffAsync(CancellationToken cancellationToken = default)
    {
        // A List (not an array): with C# 14 an array's Contains can bind to a span overload inside queries
        var staffRoleNames = new List<string> { nameof(Role.ServiceStaff), nameof(Role.Supervisor) };

        var staff = await (from user in db.Users
                           join userRole in db.UserRoles on user.Id equals userRole.UserId
                           join role in db.Roles on userRole.RoleId equals role.Id
                           where user.IsActive && staffRoleNames.Contains(role.Name!)
                           select new { user.Id, user.DisplayName })
            .AsNoTracking()
            .Distinct()
            .ToListAsync(cancellationToken);

        return staff
            .OrderBy(s => s.DisplayName)
            .Select(s => new StaffOption(s.Id, s.DisplayName))
            .ToList();
    }

    private async Task<PagedResult<RequestSummary>> PageAsync(
        IQueryable<ServiceRequest> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id, r.Reference, r.Title, Category = r.Category.Name, r.Status, r.CreatedAt,
                r.Category.TargetResolutionDays
            })
            .ToListAsync(cancellationToken);

        // Overdue (CR-001) is calculated here, never stored
        var now = time.GetUtcNow();
        var items = rows
            .Select(r => new RequestSummary(
                r.Id, r.Reference, r.Title, r.Category, r.Status, r.CreatedAt,
                OverdueRule.IsOverdue(r.Status, r.CreatedAt, r.TargetResolutionDays, now)))
            .ToList();

        return new PagedResult<RequestSummary>(items, page, pageSize, total);
    }
}
