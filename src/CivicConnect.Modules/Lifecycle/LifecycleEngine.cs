using CivicConnect.Modules.IdentityAccess;

namespace CivicConnect.Modules.Lifecycle;

/// <summary>
/// Applies one lifecycle command to a request state
/// Order of checks: authorise first, then check the table, then validate the command's data. 
/// An unauthorised caller therefore learns nothing about whether the transition would have been legal.
/// </summary>
public static class LifecycleEngine
{
    public static TransitionResult Apply(
        RequestState current, LifecycleCommand command, Actor actor, DateTimeOffset now)
    {
        // Authorise
        if (!LifecycleAuthorisation.IsAllowed(actor, current, command))
            return TransitionResult.Failed(TransitionOutcome.Forbidden, current, "Not permitted.");

        // Is this transition in the table?
        if (!LifecycleTable.TryGetNext(current.Status, command.Event, out var next))
            return TransitionResult.Failed(
                TransitionOutcome.InvalidTransition, current,
                $"{command.Event} is not allowed from {current.Status}.");

        // Validate the data this transition needs
        var validationError = Validate(command);
        if (validationError is not null)
            return TransitionResult.Failed(TransitionOutcome.ValidationFailed, current, validationError);

        // Build the new state and the audit record
        var updated = current with { Status = next, Version = current.Version + 1 };
        string? detail = null;

        switch (command.Event)
        {
            case LifecycleEvent.Reject:
                updated = updated with { RejectionReason = command.Reason!.Trim() };
                detail = updated.RejectionReason;
                break;
            case LifecycleEvent.Assign:
                updated = updated with { AssignedToId = command.TargetAssigneeId };
                detail = $"assignee={command.TargetAssigneeId}";
                break;
            case LifecycleEvent.Resolve:
                updated = updated with { ResolutionNote = command.Note!.Trim() };
                break;
            case LifecycleEvent.Close:
                updated = updated with { ClosedAt = now };
                break;
        }

        var audit = new AuditRecord(command.Event.ToString(), current.Status, next, actor.UserId, detail, now);
        return TransitionResult.Applied(updated, audit);
    }

    private static string? Validate(LifecycleCommand command) => command.Event switch
    {
        LifecycleEvent.Reject when string.IsNullOrWhiteSpace(command.Reason) => "A reason is required to reject a request.",
        LifecycleEvent.Resolve when string.IsNullOrWhiteSpace(command.Note) => "A resolution note is required to resolve a request.",
        LifecycleEvent.Assign when command.TargetAssigneeId is null => "An assignee is required.",
        _ => null
    };
}
