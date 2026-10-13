using CivicConnect.Modules.IdentityAccess;

namespace CivicConnect.Modules.Lifecycle;

/// <summary>
/// "Who may do it" column of the transition table
/// This is purely the logic, there is no UI here
/// </summary>
public static class LifecycleAuthorisation
{
    public static bool IsAllowed(Actor actor, RequestState current, LifecycleCommand command) =>
        command.Event switch
        {
            // ServiceStaff or Supervisor
            LifecycleEvent.Accept or LifecycleEvent.Reject =>
                actor.IsInRole(Role.ServiceStaff) || actor.IsInRole(Role.Supervisor),

            // Supervisor may assign anyone and ServiceStaff may assign themselves only
            LifecycleEvent.Assign =>
                actor.IsInRole(Role.Supervisor)
                || (actor.IsInRole(Role.ServiceStaff) && command.TargetAssigneeId == actor.UserId),

            // only the assignee
            LifecycleEvent.Start or LifecycleEvent.Resolve =>
                current.AssignedToId is Guid assignee && assignee == actor.UserId,

            // users with close permission
            LifecycleEvent.Close => actor.HasClosePermission,

            _ => false
        };
}
