using System.Security.Claims;
using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;

namespace CivicConnect.Web.Security;

/// <summary>
/// Role policies for broad capability (ADR-007). Per-request access is decided by
/// <see cref="RequestVisibility"/> and <see cref="LifecycleAuthorisation"/> on the server.
/// </summary>
public static class Policies
{
    public const string Requester = "Requester";
    public const string StaffQueue = "StaffQueue";

    public static readonly string[] StaffQueueRoles =
        [nameof(Role.ServiceStaff), nameof(Role.Supervisor), nameof(Role.Management)];
}

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? throw new InvalidOperationException("The signed-in user has no id claim."));

    /// <summary>The signed-in user as the <see cref="Actor"/> the rules work with.</summary>
    public static Actor ToActor(this ClaimsPrincipal user) =>
        ActorFactory.FromRoleNames(user.GetUserId(), user.FindAll(ClaimTypes.Role).Select(c => c.Value));
}

public static class StatusDisplay
{
    public static string Label(RequestStatus status) =>
        status == RequestStatus.InProgress ? "In progress" : status.ToString();

    /// <summary>One colour per kind of state: waiting, being handled, work under way, finished, stopped.</summary>
    public static string PillClass(RequestStatus status) => status switch
    {
        RequestStatus.Submitted => "cc-pill cc-pill--waiting",
        RequestStatus.Accepted or RequestStatus.Assigned => "cc-pill cc-pill--active",
        RequestStatus.InProgress => "cc-pill cc-pill--working",
        RequestStatus.Resolved or RequestStatus.Closed => "cc-pill cc-pill--done",
        RequestStatus.Rejected => "cc-pill cc-pill--stopped",
        _ => "cc-pill cc-pill--waiting"
    };

    /// <summary>The stops on the status track: the normal route, or Submitted then Rejected.</summary>
    public static IReadOnlyList<RequestStatus> TrackFor(RequestStatus status) =>
        status == RequestStatus.Rejected
            ? [RequestStatus.Submitted, RequestStatus.Rejected]
            : [RequestStatus.Submitted, RequestStatus.Accepted, RequestStatus.Assigned,
               RequestStatus.InProgress, RequestStatus.Resolved, RequestStatus.Closed];

    /// <summary>A short line that says where the request is and what happens next.</summary>
    public static string Explain(RequestStatus status) => status switch
    {
        RequestStatus.Submitted => "Waiting for a staff member to review it.",
        RequestStatus.Accepted => "Accepted. Waiting to be given to someone to work on.",
        RequestStatus.Assigned => "Assigned. Work will start soon.",
        RequestStatus.InProgress => "Work is under way.",
        RequestStatus.Resolved => "The work is done. Waiting to be closed.",
        RequestStatus.Closed => "Finished and closed.",
        RequestStatus.Rejected => "This request was not accepted.",
        _ => string.Empty
    };
}
