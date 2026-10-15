using CivicConnect.Modules.Lifecycle;

namespace CivicConnect.Modules.IdentityAccess;

/// <summary>The facts about one request that decide who may see it.</summary>
public sealed record RequestAccessFacts(Guid RequesterId, Guid? AssignedToId, RequestStatus Status);

/// <summary>
/// "Who may see this request" (ADR-007, AS-05). One rule used by every query, so lists and the
/// detail page can never disagree. A caller who may not see a request gets the same answer as for a
/// request that does not exist.
/// </summary>
public static class RequestVisibility
{
    public static bool CanView(Actor actor, RequestAccessFacts request)
    {
        // Requesters see their own requests
        if (request.RequesterId == actor.UserId)
            return true;

        // Supervisors and management see all requests
        if (actor.IsInRole(Role.Supervisor) || actor.IsInRole(Role.Management))
            return true;

        // Service staff see requests assigned to them plus unassigned Submitted/Accepted requests
        if (actor.IsInRole(Role.ServiceStaff))
            return request.AssignedToId == actor.UserId || IsInUnassignedPool(request);

        return false;
    }

    public static bool IsInUnassignedPool(RequestAccessFacts request) =>
        request.AssignedToId is null
        && request.Status is RequestStatus.Submitted or RequestStatus.Accepted;
}
