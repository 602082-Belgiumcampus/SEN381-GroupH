using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;

namespace CivicConnect.Modules.RequestQueries;

/// <summary>What a pager needs to know about any paged list.</summary>
public interface IPaged
{
    int Page { get; }
    int PageCount { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount) : IPaged
{
    public int PageCount => TotalCount == 0 ? 1 : (TotalCount + PageSize - 1) / PageSize;
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < PageCount;
}

public sealed record RequestSummary(
    Guid Id,
    string Reference,
    string Title,
    string Category,
    RequestStatus Status,
    DateTimeOffset CreatedAt,
    bool IsOverdue = false);

/// <summary>FR-008: how a list is ordered.</summary>
public enum RequestSort
{
    Oldest,
    Newest,
    RecentlyUpdated
}

/// <summary>
/// FR-008: search, filter and sort for request lists. Search matches the reference or the title,
/// ignoring case. Every value comes from the query string, so the service treats it as untrusted.
/// </summary>
public sealed record RequestFilter(
    string? Search = null,
    RequestStatus? Status = null,
    Guid? CategoryId = null,
    RequestSort Sort = RequestSort.Oldest)
{
    public const int SearchMaxLength = 100;
}

/// <summary>FR-012: one entry in a request's work log.</summary>
public sealed record WorkLogItem(string Text, string AuthorName, DateTimeOffset CreatedAt);

public sealed record HistoryItem(
    string EventType,
    RequestStatus? From,
    RequestStatus To,
    string ActorName,
    string? Detail,
    DateTimeOffset OccurredAt);

public sealed record RequestDetail(
    Guid Id,
    string Reference,
    string Title,
    string Description,
    string? Location,
    string Category,
    RequestStatus Status,
    int Version,
    Guid RequesterId,
    string RequesterName,
    Guid? AssignedToId,
    string? AssignedToName,
    string? RejectionReason,
    string? ResolutionNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<HistoryItem> History,
    IReadOnlyList<WorkLogItem> WorkLog,
    string ContactChannel = "Web",
    string? LoggedByName = null)
{
    public RequestState ToLifecycleState() =>
        new(Status, AssignedToId, Version, RejectionReason, ResolutionNote, ClosedAt);
}

/// <summary>
/// Request Queries contract (ADR-008). Every query applies <see cref="RequestVisibility"/>;
/// GetDetailAsync returns null both for a missing request and for one the actor may not see.
/// </summary>
public interface IRequestQueries
{
    Task<PagedResult<RequestSummary>> GetMyRequestsAsync(Guid requesterId, RequestFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedResult<RequestSummary>> GetStaffQueueAsync(Actor actor, RequestFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<RequestDetail?> GetDetailAsync(Guid requestId, Actor actor, CancellationToken cancellationToken = default);

    /// <summary>Active service staff and supervisors, for the supervisor's "assign to" list (FR-010).</summary>
    Task<IReadOnlyList<StaffOption>> GetAssignableStaffAsync(CancellationToken cancellationToken = default);
}

public sealed record StaffOption(Guid Id, string Name);
