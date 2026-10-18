namespace CivicConnect.Modules.Lifecycle;

// The seven request states
public enum RequestStatus
{
    Submitted,
    Accepted,
    Rejected,
    Assigned,
    InProgress,
    Resolved,
    Closed
}

// Events that can move a request between states
public enum LifecycleEvent
{
    Accept,
    Reject,
    Assign,
    Start,
    Resolve,
    Close,
    Reopen
}

public sealed record RequestState(
    RequestStatus Status,
    Guid? AssignedToId = null,
    int Version = 1,
    string? RejectionReason = null,
    string? ResolutionNote = null,
    DateTimeOffset? ClosedAt = null);

public sealed record LifecycleCommand(
    LifecycleEvent Event,
    Guid? TargetAssigneeId = null,
    string? Reason = null,
    string? Note = null);

public sealed record AuditRecord(
    string EventType,
    RequestStatus From,
    RequestStatus To,
    Guid ActorId,
    string? Detail,
    DateTimeOffset OccurredAt);

public enum TransitionOutcome
{
    Applied,
    Forbidden,
    InvalidTransition,
    ValidationFailed
}

public sealed record TransitionResult(
    TransitionOutcome Outcome,
    RequestState State,
    AuditRecord? Audit,
    string? Message)
{
    public bool Succeeded => Outcome == TransitionOutcome.Applied;

    internal static TransitionResult Applied(RequestState state, AuditRecord audit) =>
        new(TransitionOutcome.Applied, state, audit, null);

    // On any failure the original state is returned unchanged and no audit record is produced.
    internal static TransitionResult Failed(TransitionOutcome outcome, RequestState unchanged, string message) =>
        new(outcome, unchanged, null, message);
}