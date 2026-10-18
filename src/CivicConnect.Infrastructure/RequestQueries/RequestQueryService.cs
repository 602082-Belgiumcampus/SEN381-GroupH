using System.Linq.Expressions;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.RequestQueries;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.RequestQueries;

/// <summary>
/// Request Queries (FR-004, FR-005, FR-007, FR-009). Read-only and paged (NFR-005).
/// The staff queue filter is the database form of <see cref="RequestVisibility"/>;
/// the detail query applies RequestVisibility itself.
/// </summary>
public sealed class RequestQueryService(CivicConnectDbContext db) : IRequestQueries
{
    public const int MaxPageSize = 100;

    public Task<PagedResult<RequestSummary>> GetMyRequestsAsync(
        Guid requesterId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        PageAsync(
            db.ServiceRequests.Where(r => r.RequesterId == requesterId).OrderByDescending(r => r.CreatedAt),
            page, pageSize, cancellationToken);

    public Task<PagedResult<RequestSummary>> GetStaffQueueAsync(
        Actor actor, int page, int pageSize, CancellationToken cancellationToken = default)
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

        // Oldest first, so nothing waits forever at the bottom of the queue
        return PageAsync(requests.OrderBy(r => r.CreatedAt), page, pageSize, cancellationToken);
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
                r.CreatedAt, r.UpdatedAt, r.ClosedAt
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

        return new RequestDetail(
            request.Id, request.Reference, request.Title, request.Description, request.Location,
            request.Category, request.Status, request.Version,
            request.RequesterId, request.RequesterName,
            request.AssignedToId, request.AssignedToName,
            request.RejectionReason, request.ResolutionNote,
            request.CreatedAt, request.UpdatedAt, request.ClosedAt,
            history);
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

    private static async Task<PagedResult<RequestSummary>> PageAsync(
        IQueryable<ServiceRequest> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToSummary)
            .ToListAsync(cancellationToken);

        return new PagedResult<RequestSummary>(items, page, pageSize, total);
    }

    private static readonly Expression<Func<ServiceRequest, RequestSummary>> ToSummary = r =>
        new RequestSummary(r.Id, r.Reference, r.Title, r.Category.Name, r.Status, r.CreatedAt);
}
