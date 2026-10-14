using CivicConnect.Modules.IdentityAccess;

namespace CivicConnect.Modules.Lifecycle;

/// <summary>
/// Outcomes the page can show (PED Section 8.3). NotFound also covers "you may not see this request",
/// so the caller cannot tell the two apart.
/// </summary>
public enum LifecycleServiceOutcome
{
    Applied,
    NotFound,
    Forbidden,
    InvalidTransition,
    ValidationFailed,
    Conflict
}

public sealed record LifecycleServiceResult(LifecycleServiceOutcome Outcome, string? Message, int? NewVersion = null)
{
    public bool Succeeded => Outcome == LifecycleServiceOutcome.Applied;
}

/// <summary>
/// Request Lifecycle contract (ADR-003, ADR-008): the only way a request's status changes.
/// The status change and its audit entry are saved together or not at all, and
/// <paramref name="expectedVersion"/> protects against overwriting someone else's change (Section 6.5).
/// </summary>
public interface ILifecycleService
{
    Task<LifecycleServiceResult> ApplyAsync(
        Guid requestId,
        int expectedVersion,
        LifecycleCommand command,
        Actor actor,
        CancellationToken cancellationToken = default);
}
