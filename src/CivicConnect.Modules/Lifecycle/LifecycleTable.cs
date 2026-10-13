namespace CivicConnect.Modules.Lifecycle;

// The table-driven finite-state machine
// Reopening a Resolved request is not included yet

public static class LifecycleTable
{
    private static readonly Dictionary<(RequestStatus From, LifecycleEvent Event), RequestStatus> Rules = new()
    {
        [(RequestStatus.Submitted, LifecycleEvent.Accept)]  = RequestStatus.Accepted,
        [(RequestStatus.Submitted, LifecycleEvent.Reject)]  = RequestStatus.Rejected,
        [(RequestStatus.Accepted,  LifecycleEvent.Assign)]  = RequestStatus.Assigned,
        [(RequestStatus.Assigned,  LifecycleEvent.Start)]   = RequestStatus.InProgress,
        [(RequestStatus.InProgress, LifecycleEvent.Resolve)] = RequestStatus.Resolved,
        [(RequestStatus.Resolved,  LifecycleEvent.Close)]   = RequestStatus.Closed
    };

    // Read-only view of the table for tests and documentation
    public static IReadOnlyDictionary<(RequestStatus From, LifecycleEvent Event), RequestStatus> All => Rules;

    public static bool TryGetNext(RequestStatus fromStatus, LifecycleEvent lifecycleEvent, out RequestStatus next) =>
        Rules.TryGetValue((fromStatus, lifecycleEvent), out next);
}
