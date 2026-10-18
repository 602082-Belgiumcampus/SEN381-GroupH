using CivicConnect.Modules.IdentityAccess;

namespace CivicConnect.Modules.Lifecycle;

/// <summary>
/// Which lifecycle actions to offer an actor on a request (PED Section 9.3, "state-dependent actions").
/// An action is offered only if the transition table allows it from the current state AND the
/// actor is authorised for it. The server still re-checks every action when it is submitted (NFR-002).
/// </summary>
public static class LifecycleActions
{
    public static IReadOnlySet<LifecycleEvent> Available(Actor actor, RequestState state)
    {
        var available = new HashSet<LifecycleEvent>();

        foreach (var lifecycleEvent in Enum.GetValues<LifecycleEvent>())
        {
            if (!LifecycleTable.TryGetNext(state.Status, lifecycleEvent, out _))
                continue;

            // For Assign, ask "may this actor assign at all?": staff may always assign themselves.
            var probe = lifecycleEvent == LifecycleEvent.Assign
                ? new LifecycleCommand(lifecycleEvent, TargetAssigneeId: actor.UserId)
                : new LifecycleCommand(lifecycleEvent);

            if (LifecycleAuthorisation.IsAllowed(actor, state, probe))
                available.Add(lifecycleEvent);
        }

        return available;
    }

    /// <summary>Supervisors may assign any staff member; service staff may only assign themselves (T3).</summary>
    public static bool CanAssignOthers(Actor actor) => actor.IsInRole(Role.Supervisor);
}
