using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;

namespace CivicConnect.Modules.RequestQueries;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
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
    DateTimeOffset CreatedAt);

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
    IReadOnlyList<HistoryItem> History)
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
    Task<PagedResult<RequestSummary>> GetMyRequestsAsync(Guid requesterId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedResult<RequestSummary>> GetStaffQueueAsync(Actor actor, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<RequestDetail?> GetDetailAsync(Guid requestId, Actor actor, CancellationToken cancellationToken = default);

    /// <summary>Active service staff and supervisors, for the supervisor's "assign to" list (FR-010).</summary>
    Task<IReadOnlyList<StaffOption>> GetAssignableStaffAsync(CancellationToken cancellationToken = default);
}

public sealed record StaffOption(Guid Id, string Name);
