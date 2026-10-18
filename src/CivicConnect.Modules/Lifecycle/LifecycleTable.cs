namespace CivicConnect.Modules.Lifecycle;

// The table-driven finite-state machine
// T7 Reopen (OD-05, decided M4): a supervisor may send a Resolved request back to In Progress.
// Closed stays final.

public static class LifecycleTable
{
    private static readonly Dictionary<(RequestStatus From, LifecycleEvent Event), RequestStatus> Rules = new()
    {
        [(RequestStatus.Submitted, LifecycleEvent.Accept)]  = RequestStatus.Accepted,
        [(RequestStatus.Submitted, LifecycleEvent.Reject)]  = RequestStatus.Rejected,
        [(RequestStatus.Accepted,  LifecycleEvent.Assign)]  = RequestStatus.Assigned,
        [(RequestStatus.Assigned,  LifecycleEvent.Start)]   = RequestStatus.InProgress,
        [(RequestStatus.InProgress, LifecycleEvent.Resolve)] = RequestStatus.Resolved,
        [(RequestStatus.Resolved,  LifecycleEvent.Close)]   = RequestStatus.Closed,
        [(RequestStatus.Resolved,  LifecycleEvent.Reopen)]  = RequestStatus.InProgress
    };

    // Read-only view of the table for tests and documentation
    public static IReadOnlyDictionary<(RequestStatus From, LifecycleEvent Event), RequestStatus> All => Rules;

    public static bool TryGetNext(RequestStatus fromStatus, LifecycleEvent lifecycleEvent, out RequestStatus next) =>
        Rules.TryGetValue((fromStatus, lifecycleEvent), out next);
}
